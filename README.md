# REST Connector for 3CX Chat

## Overview

This project is a **REST API connector** for integrating **3CX Chat** with any API (I used Genesys v2 chat API for "Client"), designed to facilitate chat functionality and provide access to 3CX chat group statistics. Swagger is included for seamless API testing, and all logs are maintained in the `Logs/` directory for tracking interactions and diagnostics.

## Features

- **3CX Chat Integration**: Connects with [3CX’s API](https://www.3cx.com/docs/configuration-rest-api) to retrieve and manage chat statistics, enhancing your chat solution with detailed group data.
- **Client API**: Uses [Genesys v2 chat API](https://docs.genesys.com/Documentation/ES/8.5.0/WebAPI/Chat) for client-side chat functionalities. Request Chat, Send Message, Send Url, Update User Data and Chat Complete are supported.
- **3CX Chat**: For 3CX chat implementation is used [SMS/MMS API](https://www.3cx.com/docs/supported-sip-trunk-requirements/#h.z7xet9uflkmo).
- **Swagger UI**: Built-in Swagger support for easy testing and exploration of API endpoints.
- **Logging**: Centralized logging in the `Logs/` directory for tracking and troubleshooting.
- **Multiplatform**: Based on build, this application can run on Windows or Linux

## Technologies Used

- **C#**
- **ASP.NET Core**
- **3CX API** for chat statistics
- **Genesys v2 Chat API** for client-side integration
- **Swagger** for API documentation. Find swagger at: http://<RestConnectorIp:RestConnectorPort>/swagger/index.html
- **Log4Net** for logging

## Getting Started

### Prerequisites

- .NET SDK (version 8.0)
- Access to the 3CX

### Installation

1. **Clone the repository**:
    ```bash
    git clone https://github.com/JahyLuky/REST_Connector_3CX.git
    cd REST_Connector_3CX
    ```

2. **Update configuration**:<br>
   Update the configuration file (`appsettings.json`) with your 3CX API credentials, Genesys v2 chat API information, and any other relevant settings.

4. **Configuration**:  
    ```bash
    dotnet build
    ```

5. **Publish the application**:
   Windows:
    ```bash
    dotnet publish -r win-x64 --self-contained -c Release
    ```
   Linux:
    ```bash
    dotnet publish -r linux-x64 --self-contained -c Release
    ```
7. **Run the application**:<br>
   Give the "rest_connector_3cx" file execute permissions.

### Usage

1. **Start chat session**
   Request Chat -> this creates chat session in the REST Connector and created IDs.
   ```json
   {
    "tenantName": "Resources",
    "nickname":"testname123",
    "emailAddress":"email@address",
    "subject":"subject to",
    "userData":{
        "chatbot_service":"value1",
        "key2":"value2"
    }
   }
   ```
3. **Send message**
   Send Message -> this sends message to 3CX.
