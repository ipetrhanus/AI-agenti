# Task3: C# AI Agent s OpenAI a Wikipedia nástroji

AI agent implementovaný v C# s použitím OpenAI .NET SDK.

## Funkce

Agent používá dva nástroje (tools):
- **get_city_by_psc** - Vyhledá město podle PSČ v lokální databázi
- **get_wikipedia_info** - Získá základní informace o městě z české Wikipedie

## Instalace

1. Nainstalujte .NET 10 SDK
2. Nastavte environment variable s OpenAI API klíčem:

```bash
# Windows (PowerShell)
$env:OPENAI_API_KEY="your-api-key-here"

# Windows (CMD)
set OPENAI_API_KEY=your-api-key-here

# Linux/Mac
export OPENAI_API_KEY=your-api-key-here
```

3. Spusťte aplikaci:

```bash
cd MicrosoftAgentFramework
dotnet run
```

## Příklady použití

- "Jaké město má PSČ 60200?"
- "Co víš o Praze?"
- "Jaké město má PSČ 11000 a co o něm víš?"
