/*
Zkusil jsem udlěat jednoduchého agenta, který umí vyhledávat PSČ v České republice a získávat informace o městech z české Wikipedie.

Jako framework jsem použil OpenAI .NET SDK.

Agent používá dva nástroje:
-jeden pro vyhledání města podle PSČ ve slovníku
-druhý pro získání informací z Wikipedie.
*/

using OpenAI;
using OpenAI.Chat;
using System.Text.Json;
using System.Text;

class Program
{
    // Dictionary s PSČ a městy
    private static readonly Dictionary<string, string> PscDatabase = new()
    {
        { "11000", "Praha 1" },
        { "12000", "Praha 2" },
        { "60200", "Brno" },
        { "70200", "Ostrava" },
        { "30100", "Plzeň" },
        { "37001", "České Budějovice" },
        { "50002", "Hradec Králové" },
        { "77900", "Olomouc" },
        { "40001", "Ústí nad Labem" },
        { "46001", "Liberec" },
        { "54901", "Nové Město nad Metují" }
    };

    static async Task Main(string[] args)
    {
        // Načteme API klíč z environment variable
        var API_KEY = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
            ?? throw new Exception("OPENAI_API_KEY environment variable is not set");
        var deploymentName = "gpt-4o-mini";

        // Používáme čistý OpenAI klient (ne Azure)
        OpenAIClient openAIClient = new OpenAIClient(API_KEY);
        var chatClient = openAIClient.GetChatClient(deploymentName);

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage("Jsi chytrý asistent, který umí vyhledávat PSČ v České republice a informace o městech z Wikipedie. " +
                "Pokud uživatel zadá PSČ, použij nástroj get_city_by_psc. " +
                "Pokud se ptá na informace o městě, použij nástroj get_wikipedia_info.")
        };

        // Definice nástrojů
        var tools = new List<ChatTool>
        {
            ChatTool.CreateFunctionTool(
                functionName: "get_city_by_psc",
                functionDescription: "Vyhledá město podle PSČ v databázi českých měst",
                functionParameters: BinaryData.FromString("""
                {
                    "type": "object",
                    "properties": {
                        "psc": {
                            "type": "string",
                            "description": "PSČ ve formátu 5 číslic (např. '11000')"
                        }
                    },
                    "required": ["psc"]
                }
                """)
            ),
            ChatTool.CreateFunctionTool(
                functionName: "get_wikipedia_info",
                functionDescription: "Vyhledá základní informace o městě na české Wikipedii",
                functionParameters: BinaryData.FromString("""
                {
                    "type": "object",
                    "properties": {
                        "city_name": {
                            "type": "string",
                            "description": "Název města (např. 'Praha', 'Brno')"
                        }
                    },
                    "required": ["city_name"]
                }
                """)
            )
        };

        Console.WriteLine("Konverzační mód s vyhledáváním PSČ a informací z Wikipedie. Prázdný řádek = konec.");
        Console.WriteLine("Zkuste např: 'Jaké město má PSČ 60200?' nebo 'Co víš o Praze?'\n");

        while (true)
        {
            Console.Write("> ");
            var userInput = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(userInput))
                break;

            // Přidáme user message
            messages.Add(new UserChatMessage(userInput));

            bool continueLoop = true;
            while (continueLoop)
            {
                // Získáme odpověď od AI s tools
                var chatOptions = new ChatCompletionOptions();
                foreach (var tool in tools)
                {
                    chatOptions.Tools.Add(tool);
                }

                var completion = await chatClient.CompleteChatAsync(messages, chatOptions);
                var responseMessage = completion.Value;

                // Přidáme assistant message do historie
                messages.Add(new AssistantChatMessage(responseMessage));

                // Zkontrolujeme, zda AI chce volat nějaký tool
                if (responseMessage.FinishReason == ChatFinishReason.ToolCalls)
                {
                    foreach (var toolCall in responseMessage.ToolCalls)
                    {
                        if (toolCall.FunctionName == "get_city_by_psc")
                        {
                            // Parsujeme argumenty
                            var arguments = JsonDocument.Parse(toolCall.FunctionArguments).RootElement;
                            var psc = arguments.GetProperty("psc").GetString() ?? "";

                            // Voláme naši funkci
                            var result = GetCityByPsc(psc);
                            Console.WriteLine($"[Tool Call: Vyhledávám PSČ {psc}...]");

                            // Přidáme výsledek tool callu do konverzace
                            messages.Add(new ToolChatMessage(toolCall.Id, result));
                        }
                        else if (toolCall.FunctionName == "get_wikipedia_info")
                        {
                            // Parsujeme argumenty
                            var arguments = JsonDocument.Parse(toolCall.FunctionArguments).RootElement;
                            var cityName = arguments.GetProperty("city_name").GetString() ?? "";

                            // Voláme naši funkci
                            Console.WriteLine($"[Tool Call: Vyhledávám informace o městě {cityName} na Wikipedii...]");
                            var result = await GetWikipediaInfo(cityName);

                            // Přidáme výsledek tool callu do konverzace
                            messages.Add(new ToolChatMessage(toolCall.Id, result));
                        }
                    }
                    // Pokračujeme v cyklu, aby AI mohla odpovědět s výsledky
                }
                else
                {
                    // AI odpověděla bez tool callů, zobrazíme odpověď
                    var response = responseMessage.Content[0].Text;
                    Console.WriteLine();
                    Console.WriteLine("🧠 Agent:");
                    Console.WriteLine(response);
                    Console.WriteLine();
                    continueLoop = false;
                }
            }
        }
    }

    // Funkce pro vyhledání města podle PSČ
    static string GetCityByPsc(string psc)
    {
        if (PscDatabase.TryGetValue(psc, out var city))
        {
            return $"PSČ {psc} patří městu: {city}";
        }
        else
        {
            return $"PSČ {psc} nebylo nalezeno v databázi.";
        }
    }

    // Funkce pro získání informací z Wikipedie
    static async Task<string> GetWikipediaInfo(string cityName)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "MicrosoftAgentFramework/1.0");

            // Wikipedia API - extract endpoint pro získání úvodního textu
            var url = $"https://cs.wikipedia.org/api/rest_v1/page/summary/{Uri.EscapeDataString(cityName)}";

            var response = await httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return $"Nepodařilo se najít informace o městě '{cityName}' na Wikipedii.";
            }

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);

            // Získáme základní informace
            var title = doc.RootElement.GetProperty("title").GetString();
            var extract = doc.RootElement.GetProperty("extract").GetString();

            // Pokusíme se získat i typ (např. "město v České republice")
            var description = doc.RootElement.TryGetProperty("description", out var descProp)
                ? descProp.GetString()
                : "";

            var result = new StringBuilder();
            result.AppendLine($"=== {title} ===");
            if (!string.IsNullOrEmpty(description))
            {
                result.AppendLine($"({description})");
                result.AppendLine();
            }
            result.AppendLine(extract);

            return result.ToString();
        }
        catch (Exception ex)
        {
            return $"Chyba při získávání informací z Wikipedie: {ex.Message}";
        }
    }
}
