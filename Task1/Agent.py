# Agent.py - Invoice Validation Script

import os
import json
from datetime import datetime
from openai import OpenAI

# Initialize OpenAI client
client = OpenAI(api_key=os.getenv("OPENAI_API_KEY"))

MAX_ITERATIONS = 10

# Validation data
VALID_SUPPLIERS = {
    "Dodavatel s.r.o., Hlavní 123, Praha 1": True,
    "ABC Company, Nová 45, Brno": True,
    "XYZ s.r.o., Dlouhá 78, Ostrava": True
}

VALID_CUSTOMERS = {
    "Odběratel a.s., Krátká 10, Praha 2": True,
    "Firma ABC, Zelená 25, Brno": True,
    "Společnost XYZ, Modrá 5, Ostrava": True
}

VALID_ACCOUNT_NUMBERS = {
    "123456789/0100s": True,
    "987654321/0800s": True,
    "555666777/2010s": True
}

VALID_ORDER_NUMBERS = {
    "OBJ-2024-001": True,
    "OBJ-2024-002": True,
    "OBJ-2024-003": True
}

# Validation functions
def check_supplier_address(address: str) -> str:
    """Kontrola adresy dodavatele"""
    if address in VALID_SUPPLIERS:
        return json.dumps({"valid": True, "message": "Adresa dodavatele je platná"})
    return json.dumps({"valid": False, "message": "Adresa dodavatele není v systému"})

def check_customer_address(address: str) -> str:
    """Kontrola adresy odběratele"""
    if address in VALID_CUSTOMERS:
        return json.dumps({"valid": True, "message": "Adresa odběratele je platná"})
    return json.dumps({"valid": False, "message": "Adresa odběratele není v systému"})

def check_account_number(account_number: str) -> str:
    """Kontrola čísla účtu"""
    if account_number in VALID_ACCOUNT_NUMBERS:
        return json.dumps({"valid": True, "message": "Číslo účtu je platné"})
    return json.dumps({"valid": False, "message": "Číslo účtu není v systému"})

def check_order_number(order_number: str) -> str:
    """Kontrola čísla objednávky"""
    if order_number in VALID_ORDER_NUMBERS:
        return json.dumps({"valid": True, "message": "Číslo objednávky je platné"})
    return json.dumps({"valid": False, "message": "Číslo objednávky není v systému"})

def check_due_date(due_date: str) -> str:
    """Kontrola data splatnosti"""
    try:
        due_date_obj = datetime.strptime(due_date, "%Y-%m-%d")
        if due_date_obj > datetime.now():
            return json.dumps({"valid": True, "message": "Datum splatnosti je platné"})
        return json.dumps({"valid": False, "message": "Faktura je po splatnosti"})
    except (ValueError, TypeError) as e:
        return json.dumps({"valid": False, "message": f"Neplatný formát data: {str(e)}"})


# Tool definitions
tools = [
    {
        "type": "function",
        "function": {
            "name": "check_supplier_address",
            "description": "Kontroluje, zda je adresa dodavatele platná",
            "parameters": {
                "type": "object",
                "properties": {
                    "address": {
                        "type": "string",
                        "description": "Adresa dodavatele z faktury"
                    }
                },
                "required": ["address"]
            }
        }
    },
    {
        "type": "function",
        "function": {
            "name": "check_customer_address",
            "description": "Kontroluje, zda je adresa odběratele platná",
            "parameters": {
                "type": "object",
                "properties": {
                    "address": {
                        "type": "string",
                        "description": "Adresa odběratele z faktury"
                    }
                },
                "required": ["address"]
            }
        }
    },
    {
        "type": "function",
        "function": {
            "name": "check_account_number",
            "description": "Kontroluje, zda je číslo účtu platné",
            "parameters": {
                "type": "object",
                "properties": {
                    "account_number": {
                        "type": "string",
                        "description": "Číslo bankovního účtu z faktury"
                    }
                },
                "required": ["account_number"]
            }
        }
    },
    {
        "type": "function",
        "function": {
            "name": "check_order_number",
            "description": "Kontroluje, zda je číslo objednávky platné",
            "parameters": {
                "type": "object",
                "properties": {
                    "order_number": {
                        "type": "string",
                        "description": "Číslo objednávky z faktury"
                    }
                },
                "required": ["order_number"]
            }
        }
    },
    {
        "type": "function",
        "function": {
            "name": "check_due_date",
            "description": "Kontroluje, zda je datum splatnosti platný",
            "parameters": {
                "type": "object",
                "properties": {
                    "due_date": {
                        "type": "string",
                        "description": "Datum splatnosti z faktury ve formátu YYYY-MM-DD"
                    }
                },
                "required": ["due_date"]
            }
        }
    }
]

# Map function names to actual functions
available_functions = {
    "check_supplier_address": check_supplier_address,
    "check_customer_address": check_customer_address,
    "check_account_number": check_account_number,
    "check_order_number": check_order_number,
    "check_due_date": check_due_date,
}

# Sample invoice text
invoice_text = """
FAKTURA

Dodavatel:
Dodavatel s.r.o., Hlavní 123, Praha 1

Odběratel:
Odběratel a.s., Krátká 10, Praha 2

Číslo objednávky: OBJ-2024-001
Číslo účtu: 123456789/0100

Položky:
- Služby IT: 10000 Kč
- Konzultace: 5000 Kč

Celkem: 15000 Kč
"""

print("=== Invoice Validation Agent ===\n")
print("Zpracovávám fakturu...\n")

# Initialize conversation
messages = [
    {
        "role": "system",
        "content": "Jsi validační agent pro faktury. Tvým úkolem je nejprve rozpoznat, že jde o fakturu, a poté zkontrolovat všechny důležité údaje pomocí dostupných nástrojů: adresu dodavatele, adresu odběratele, číslo účtu a číslo objednávky. Na konci vyhodnoť, zda je faktura v pořádku nebo ne."
    },
    {
        "role": "user",
        "content": f"Zkontroluj prosím tuto fakturu:\n\n{invoice_text}"
    }
]

iteration = 0
while iteration < MAX_ITERATIONS:
    iteration += 1
    print(f"Iterace {iteration}:")

    # Call OpenAI API
    response = client.chat.completions.create(
        model="gpt-4o",
        messages=messages,
        tools=tools,
        tool_choice="auto"
    )

    response_message = response.choices[0].message
    tool_calls = response_message.tool_calls

    # Add assistant's response to messages
    messages.append(response_message)

    # Check if agent wants to call functions
    if tool_calls:
        print(f"Agent volá {len(tool_calls)} nástrojů...")

        for tool_call in tool_calls:
            function_name = tool_call.function.name
            function_args = json.loads(tool_call.function.arguments)

            print(f"  - {function_name}({function_args})")

            # Call the function
            function_response = available_functions[function_name](**function_args)

            # Add function response to messages
            messages.append({
                "role": "tool",
                "tool_call_id": tool_call.id,
                "name": function_name,
                "content": function_response
            })
    else:
        # No more tool calls, agent has finished
        print(f"\n{response_message.content}\n")
        break

    print()

print("=== Validace dokončena ===")
