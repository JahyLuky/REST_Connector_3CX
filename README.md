# REST Connector for 3CX Chat

## Overview

This project is a **REST API connector** for integrating **3CX Chat** with any API (I used Genesys v2 chat API for "Client"), designed to facilitate chat functionality and provide access to 3CX chat group statistics. Swagger is included for seamless API testing, and all logs are maintained in the `Logs/` directory for tracking interactions and diagnostics.

## Features

- **3CX Chat Integration**: Connects with 3CX’s API to retrieve and manage chat statistics, enhancing your chat solution with detailed group data.
- **Client API**: Uses Genesys v2 chat API for client-side chat functionalities. Feel free to change it to your needs.
- **Swagger UI**: Built-in Swagger support for easy testing and exploration of API endpoints.
- **Logging**: Centralized logging in the `Logs/` directory for tracking and troubleshooting.
- **Multiplatform**: Based on build, this application can run on Windows or Linux

## Technologies Used

- **C#**
- **ASP.NET Core**
- **3CX API** for chat statistics
- **Genesys v2 Chat API** for client-side integration
- **Swagger** for API documentation
- **Log4Net** for logging

## Getting Started

### Prerequisites

- .NET SDK (version 8.0)
- Access to the 3CX

### Installation

1. **Clone the repository**:
    ```bash
    git clone [https://github.com/yourusername/yourprojectname.git](https://github.com/JahyLuky/REST_Connector_3CX.git)
    cd rest_connector_3cx
    ```

2. **Install dependencies**:
   Update the configuration file (`appsettings.json`) with your 3CX API credentials, Genesys v2 chat API information, and any other relevant settings.

4. **Configuration**:  
    ```bash
    dotnet build
    ```

5. **Publish the application**:
   for Windows:
    ```bash
    dotnet publish -r win-x64 --self-contained -c Release
    ```
   for Linux:
    ```bash
    dotnet publish -r linux-x64 --self-contained -c Release
    ```
7. ****
   Give the "rest_connector_3cx" file execute permissions.


