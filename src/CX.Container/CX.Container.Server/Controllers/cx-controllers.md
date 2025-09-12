# CX Controllers API Documentation

## Overview
This document provides comprehensive API documentation for all CX controllers in the VectorMind Server application. Each controller is documented with its endpoints, request/response formats, and authentication requirements.

## Table of Contents
1. [QA Controller](#qa-controller)
2. [Menu Assistant Config Controller](#menu-assistant-config-controller)
3. [SQL Query Assistant Config Controller](#sql-query-assistant-config-controller)
4. [SQL Report Assistant Config Controller](#sql-report-assistant-config-controller)
5. [Vector Link Importer Controller](#vector-link-importer-controller)
6. [Pinecone Archive Controller](#pinecone-archive-controller)
7. [Anything to Markdown Converter Controller](#anything-to-markdown-converter-controller)
8. [Any Config Controller](#any-config-controller)
9. [IAssistant Controller](#iassistant-controller)
10. [Duoporta Controller](#duoporta-controller)
11. [Channel Controller](#channel-controller)
12. [Walter1 Assistants Controller](#walter1-assistants-controller)
13. [Text to Schema Controller](#text-to-schema-controller)
14. [PostgreSQL Controller](#postgresql-controller)
15. [PDF Controller](#pdf-controller)
16. [Lua Controller](#lua-controller)
17. [JSON Table Controller](#json-table-controller)
18. [GPT Vision Extractor Controller](#gpt-vision-extractor-controller)
19. [Flat Query Assistant Controller](#flat-query-assistant-controller)
20. [DocX to PDF Controller](#docx-to-pdf-controller)
21. [Call Analyzer Controller](#call-analyzer-controller)
22. [Walter1 Assistants Config Controller](#walter1-assistants-config-controller)
23. [VectorMind Live Assistants Config Controller](#vectormind-live-assistants-config-controller)
24. [Text to Schema Assistants Config Controller](#text-to-schema-assistants-config-controller)
25. [Scheduled Question Agents Config Controller](#scheduled-question-agents-config-controller)
26. [PostgreSQL Clients Config Controller](#postgresql-clients-config-controller)
27. [Lua Core Config Controller](#lua-core-config-controller)
28. [JSON Schemas Config Controller](#json-schemas-config-controller)
29. [Flat Query Assistants Config Controller](#flat-query-assistants-config-controller)
30. [Pinecones Config Controller](#pinecones-config-controller)
31. [Pinecone Namespaces Config Controller](#pinecone-namespaces-config-controller)
32. [Channels Config Controller](#channels-config-controller)

## QA Controller

### Base URL
```
/api/QA
```

### Endpoints

#### Evaluate QA Document
```http
POST /api/QA/eval
```

Evaluates a QA document using the specified assistant.

**Authentication:** Anonymous (requires API key or permission)

**Request Parameters:**
- `file` (Form Data): Excel file containing QA entries
- `assistantName` (Query): Name of the assistant to use for evaluation

**Response:**
- Success (200 OK): Excel file with evaluation results
- Bad Request (400): Invalid input or processing error
- Unauthorized (401): Missing or invalid permissions

**Headers:**
- `Content-Disposition`: Attachment with filename
- `X-Content-Type-Options`: nosniff
- `Cache-Control`: no-cache, no-store, must-revalidate
- `Pragma`: no-cache
- `Expires`: 0

**Constraints:**
- Maximum concurrent evaluations: 1
- Timeout: 60 minutes
- Progress updates every 15 seconds
- Maximum retries: 3
- Delay between retries: 2-30 seconds

## Menu Assistant Config Controller

### Base URL
```
/api/config/menu-assistant
```

### Endpoints

#### Get All Configurations
```http
GET /api/config/menu-assistant
```

Retrieves all menu assistant configurations.

**Authentication:** Requires API key or permission

**Response:**
- Success (200 OK): List of configurations
- Unauthorized (401): Missing or invalid permissions

#### Get Configuration by ID
```http
GET /api/config/menu-assistant/{id}
```

Retrieves a specific menu assistant configuration.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Configuration ID

**Response:**
- Success (200 OK): Configuration object
- Bad Request (400): Invalid ID
- Not Found (404): Configuration not found
- Unauthorized (401): Missing or invalid permissions

#### Create/Update Configuration
```http
PUT /api/config/menu-assistant/{id}
```

Creates or updates a menu assistant configuration.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Configuration ID
- `config` (Body): Configuration object

**Response:**
- Success (204 No Content): Configuration created/updated
- Bad Request (400): Invalid input
- Unauthorized (401): Missing or invalid permissions

#### Delete Configuration
```http
DELETE /api/config/menu-assistant/{id}
```

Deletes a menu assistant configuration.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Configuration ID

**Response:**
- Success (204 No Content): Configuration deleted
- Bad Request (400): Invalid ID
- Unauthorized (401): Missing or invalid permissions

## SQL Query Assistant Config Controller

### Base URL
```
/api/config/sql-query-assistant
```

### Endpoints

#### Get All Configurations
```http
GET /api/config/sql-query-assistant
```

Retrieves all SQL query assistant configurations.

**Authentication:** Requires API key or permission

**Response:**
- Success (200 OK): List of configurations
- Unauthorized (401): Missing or invalid permissions

#### Get Configuration by ID
```http
GET /api/config/sql-query-assistant/{id}
```

Retrieves a specific SQL query assistant configuration.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Configuration ID

**Response:**
- Success (200 OK): Configuration object
- Bad Request (400): Invalid ID
- Not Found (404): Configuration not found
- Unauthorized (401): Missing or invalid permissions

#### Create/Update Configuration
```http
PUT /api/config/sql-query-assistant/{id}
```

Creates or updates a SQL query assistant configuration.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Configuration ID
- `config` (Body): Configuration object

**Response:**
- Success (204 No Content): Configuration created/updated
- Bad Request (400): Invalid input
- Unauthorized (401): Missing or invalid permissions

#### Delete Configuration
```http
DELETE /api/config/sql-query-assistant/{id}
```

Deletes a SQL query assistant configuration.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Configuration ID

**Response:**
- Success (204 No Content): Configuration deleted
- Bad Request (400): Invalid ID
- Unauthorized (401): Missing or invalid permissions

## SQL Report Assistant Config Controller

### Base URL
```
/api/config/sql-report-assistant
```

### Endpoints

#### Get All Configurations
```http
GET /api/config/sql-report-assistant
```

Retrieves all SQL report assistant configurations.

**Authentication:** Requires API key or permission

**Response:**
- Success (200 OK): List of configurations
- Unauthorized (401): Missing or invalid permissions

#### Get Configuration by ID
```http
GET /api/config/sql-report-assistant/{id}
```

Retrieves a specific SQL report assistant configuration.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Configuration ID

**Response:**
- Success (200 OK): Configuration object
- Bad Request (400): Invalid ID
- Not Found (404): Configuration not found
- Unauthorized (401): Missing or invalid permissions

#### Create/Update Configuration
```http
PUT /api/config/sql-report-assistant/{id}
```

Creates or updates a SQL report assistant configuration.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Configuration ID
- `config` (Body): Configuration object

**Response:**
- Success (204 No Content): Configuration created/updated
- Bad Request (400): Invalid input
- Unauthorized (401): Missing or invalid permissions

#### Delete Configuration
```http
DELETE /api/config/sql-report-assistant/{id}
```

Deletes a SQL report assistant configuration.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Configuration ID

**Response:**
- Success (204 No Content): Configuration deleted
- Bad Request (400): Invalid ID
- Unauthorized (401): Missing or invalid permissions

## Common Authentication

All endpoints require either:
1. A valid API key in the `X-API-Key` header
2. User authentication with appropriate permissions

## Common Error Responses

### 400 Bad Request
```json
{
    "message": "Error description"
}
```

### 401 Unauthorized
```json
{
    "message": "Not authorized"
}
```

### 404 Not Found
```json
{
    "message": "Resource not found"
}
```

### 500 Internal Server Error
```json
{
    "message": "Internal server error"
}
```

## Rate Limiting

- Maximum concurrent evaluations: 1
- Request timeout: 60 minutes
- Progress updates: Every 15 seconds
- Maximum retries: 3
- Retry delay: 2-30 seconds

## Security Headers

All responses include the following security headers:
- `X-Content-Type-Options: nosniff`
- `Cache-Control: no-cache, no-store, must-revalidate`
- `Pragma: no-cache`
- `Expires: 0`

## Vector Link Importer Controller

### Base URL
```
/api/VectorLinkImporter
```

### Endpoints

#### Import Document
```http
POST /api/VectorLinkImporter/import
```

Imports a document with optional attachments and metadata.

**Authentication:** Anonymous (requires API key or permission)

**Request Parameters:**
- `file` (Form Data): Main document file to import
- `description` (Form Data, optional): Description of the document
- `documentId` (Form Data, optional): Custom document ID (generated if not provided)
- `attachments` (Form Data, optional): List of additional files to attach
- `tags` (Form Data, optional): Set of tags for the document
- `sourceDocumentDisplayName` (Form Data, optional): Display name for the source document
- `channels` (Form Data, optional): Array of channels to associate with the document
- `extractImages` (Form Data, optional): Whether to extract images from the document
- `preferImageTextExtraction` (Form Data, optional): Whether to prefer image text extraction
- `trainCitations` (Form Data, optional): Whether to train citations
- `attachToSelf` (Form Data, optional): Whether to attach to self
- `attachPageImages` (Form Data, optional): Whether to attach page images
- `archive` (Form Data, optional): Archive identifier

**Response:**
- Success (200 OK): Import details including document ID and metadata
- Bad Request (400): Invalid input or processing error
- Unauthorized (401): Missing or invalid permissions

**Example Response:**
```json
{
    "message": "Import completed successfully",
    "documentId": "guid",
    "totalChunks": 10,
    "fileName": "example.pdf",
    "filePath": "temp/path",
    "attachments": [
        { "fileName": "attachment1.pdf" }
    ],
    "description": "Document description",
    "tags": ["tag1", "tag2"],
    "channels": ["channel1"],
    "extractImages": true,
    "preferImageTextExtraction": false,
    "trainCitations": true,
    "attachToSelf": false,
    "attachPageImages": true,
    "archive": "archive1"
}
```

#### Delete Document
```http
DELETE /api/VectorLinkImporter/{documentId}
```

Deletes a document by its ID.

**Authentication:** Anonymous (requires API key or permission)

**Parameters:**
- `documentId` (Path): GUID of the document to delete

**Response:**
- Success (200 OK): Deletion confirmation
- Bad Request (400): Invalid document ID
- Not Found (404): Document not found
- Unauthorized (401): Missing or invalid permissions

**Example Response:**
```json
{
    "message": "Document deleted successfully",
    "documentId": "guid"
}
```

## Pinecone Archive Controller

### Base URL
```
/api/PineconeArchive
```

### Endpoints

#### Register Multiple Chunks
```http
POST /api/PineconeArchive/register
```

Registers multiple text chunks for a document in the Pinecone archive.

**Authentication:** Requires API key or permission

**Request Body:**
```json
{
    "documentId": "guid",
    "chunks": [
        {
            "text": "chunk text",
            "metadata": {
                "key": "value"
            }
        }
    ],
    "namespace": "optional-namespace"
}
```

**Response:**
- Success (200 OK): Registration confirmation
- Bad Request (400): Invalid input
- Unauthorized (401): Missing or invalid permissions
- Internal Server Error (500): Processing error

**Example Response:**
```json
{
    "message": "Successfully registered 5 chunks"
}
```

#### Register Single Chunk
```http
POST /api/PineconeArchive/register/single
```

Registers a single text chunk in the Pinecone archive.

**Authentication:** Requires API key or permission

**Request Body:**
```json
{
    "chunk": {
        "text": "chunk text",
        "metadata": {
            "key": "value"
        }
    },
    "namespace": "optional-namespace"
}
```

**Response:**
- Success (200 OK): Registration confirmation
- Bad Request (400): Invalid input
- Unauthorized (401): Missing or invalid permissions
- Internal Server Error (500): Processing error

**Example Response:**
```json
{
    "message": "Successfully registered chunk"
}
```

#### Remove Document
```http
DELETE /api/PineconeArchive/document/{documentId}
```

Removes all chunks associated with a document from the Pinecone archive.

**Authentication:** Requires API key or permission

**Parameters:**
- `documentId` (Path): GUID of the document to remove

**Response:**
- Success (200 OK): Removal confirmation
- Bad Request (400): Invalid document ID
- Internal Server Error (500): Processing error

**Example Response:**
```json
{
    "message": "Successfully removed document guid"
}
```

#### Clear Archive
```http
DELETE /api/PineconeArchive/clear
```

Clears all chunks from the Pinecone archive, optionally filtered by namespace.

**Authentication:** Requires API key or permission

**Parameters:**
- `ns` (Query, optional): Namespace to clear. If not provided, clears all namespaces.

**Response:**
- Success (200 OK): Clear confirmation
- Unauthorized (401): Missing or invalid permissions
- Internal Server Error (500): Processing error

**Example Response:**
```json
{
    "message": "Successfully cleared archive"
}
```

## Anything to Markdown Converter Controller

### Base URL
```
/api/MarkdownConverter
```

### Endpoints

#### Convert PDF to Markdown
```http
POST /api/MarkdownConverter/convert/pdf
```

Converts a PDF file to Markdown format.

**Request Parameters:**
- `file` (Form Data): PDF file to convert

**Response:**
- Success (200 OK): Markdown content with appropriate headers
- Bad Request (400): Invalid file format or empty file
- Internal Server Error (500): Processing error

**Headers:**
- `Content-Disposition`: Attachment with filename
- `Content-Type`: text/markdown; charset=utf-8

#### Convert PowerPoint to Markdown
```http
POST /api/MarkdownConverter/convert/powerpoint
```

Converts a PowerPoint file to Markdown format.

**Request Parameters:**
- `file` (Form Data): PowerPoint file to convert (.ppt, .pptx)

**Response:**
- Success (200 OK): Markdown content with appropriate headers
- Bad Request (400): Invalid file format or empty file
- Internal Server Error (500): Processing error

**Headers:**
- `Content-Disposition`: Attachment with filename
- `Content-Type`: text/markdown; charset=utf-8

#### Convert Word to Markdown
```http
POST /api/MarkdownConverter/convert/word
```

Converts a Word document to Markdown format.

**Request Parameters:**
- `file` (Form Data): Word file to convert (.doc, .docx)

**Response:**
- Success (200 OK): Markdown content with appropriate headers
- Bad Request (400): Invalid file format or empty file
- Internal Server Error (500): Processing error

**Headers:**
- `Content-Disposition`: Attachment with filename
- `Content-Type`: text/markdown; charset=utf-8

#### Convert Excel to Markdown
```http
POST /api/MarkdownConverter/convert/excel
```

Converts an Excel file to Markdown format.

**Request Parameters:**
- `file` (Form Data): Excel file to convert (.xls, .xlsx, .csv)

**Response:**
- Success (200 OK): Markdown content with appropriate headers
- Bad Request (400): Invalid file format or empty file
- Internal Server Error (500): Processing error

**Headers:**
- `Content-Disposition`: Attachment with filename
- `Content-Type`: text/markdown; charset=utf-8

#### Convert Image to Markdown
```http
POST /api/MarkdownConverter/convert/image
```

Converts an image file to Markdown format.

**Request Parameters:**
- `file` (Form Data): Image file to convert (.jpg, .jpeg, .png, .gif, .bmp)

**Response:**
- Success (200 OK): Markdown content with appropriate headers
- Bad Request (400): Invalid file format or empty file
- Internal Server Error (500): Processing error

**Headers:**
- `Content-Disposition`: Attachment with filename
- `Content-Type`: text/markdown; charset=utf-8

#### Convert Text to Markdown
```http
POST /api/MarkdownConverter/convert/text
```

Converts a text file to Markdown format.

**Request Parameters:**
- `file` (Form Data): Text file to convert (.txt, .csv, .json, .xml)

**Response:**
- Success (200 OK): Markdown content with appropriate headers
- Bad Request (400): Invalid file format or empty file
- Internal Server Error (500): Processing error

**Headers:**
- `Content-Disposition`: Attachment with filename
- `Content-Type`: text/markdown; charset=utf-8

#### Convert Archive to Markdown
```http
POST /api/MarkdownConverter/convert/archive
```

Converts a ZIP archive to Markdown format.

**Request Parameters:**
- `file` (Form Data): ZIP archive to convert (.zip)

**Response:**
- Success (200 OK): Markdown content with appropriate headers
- Bad Request (400): Invalid file format or empty file
- Internal Server Error (500): Processing error

**Headers:**
- `Content-Disposition`: Attachment with filename
- `Content-Type`: text/markdown; charset=utf-8

#### Convert HTML to Markdown
```http
POST /api/MarkdownConverter/convert/html
```

Converts an HTML file to Markdown format.

**Request Parameters:**
- `file` (Form Data): HTML file to convert (.html, .htm)

**Response:**
- Success (200 OK): Markdown content with appropriate headers
- Bad Request (400): Invalid file format or empty file
- Internal Server Error (500): Processing error

**Headers:**
- `Content-Disposition`: Attachment with filename
- `Content-Type`: text/markdown; charset=utf-8

#### Convert Any File to Markdown
```http
POST /api/MarkdownConverter/convert
```

Converts any supported file type to Markdown format.

**Request Parameters:**
- `file` (Form Data): File to convert (any supported format)

**Supported Formats:**
- PDF (.pdf)
- PowerPoint (.ppt, .pptx)
- Word (.doc, .docx)
- Excel (.xls, .xlsx, .csv)
- Images (.jpg, .jpeg, .png, .gif, .bmp)
- Text (.txt, .csv, .json, .xml)
- Archives (.zip)
- HTML (.html, .htm)

**Response:**
- Success (200 OK): Markdown content with appropriate headers
- Bad Request (400): Invalid file format or empty file
- Internal Server Error (500): Processing error

**Headers:**
- `Content-Disposition`: Attachment with filename
- `Content-Type`: text/markdown; charset=utf-8

## Any Config Controller

### Base URL
```
/api/config/any-config
```

### Endpoints

#### Get All Configurations
```http
GET /api/config/any-config
```

Retrieves all any config configurations.

**Authentication:** Requires API key or permission

**Response:**
- Success (200 OK): List of configurations
- Unauthorized (401): Missing or invalid permissions

#### Get Configuration by ID
```http
GET /api/config/any-config/{id}
```

Retrieves a specific any config configuration.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Configuration ID

**Response:**
- Success (200 OK): Configuration object
- Bad Request (400): Invalid ID
- Not Found (404): Configuration not found
- Unauthorized (401): Missing or invalid permissions

#### Create/Update Configuration
```http
PUT /api/config/any-config/{id}
```

Creates or updates a any config configuration.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Configuration ID
- `config` (Body): Configuration object

**Response:**
- Success (204 No Content): Configuration created/updated
- Bad Request (400): Invalid input
- Unauthorized (401): Missing or invalid permissions

#### Delete Configuration
```http
DELETE /api/config/any-config/{id}
```

Deletes a any config configuration.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Configuration ID

**Response:**
- Success (204 No Content): Configuration deleted
- Bad Request (400): Invalid ID
- Unauthorized (401): Missing or invalid permissions

## IAssistant Controller

### Base URL
```
/api/IAssistant
```

### Endpoints

#### Get All Assistants
```http
GET /api/IAssistant
```

Retrieves all assistants.

**Authentication:** Requires API key or permission

**Response:**
- Success (200 OK): List of assistants
- Unauthorized (401): Missing or invalid permissions

#### Get Assistant by ID
```http
GET /api/IAssistant/{id}
```

Retrieves a specific assistant.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Assistant ID

**Response:**
- Success (200 OK): Assistant object
- Bad Request (400): Invalid ID
- Not Found (404): Assistant not found
- Unauthorized (401): Missing or invalid permissions

#### Create/Update Assistant
```http
PUT /api/IAssistant/{id}
```

Creates or updates an assistant.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Assistant ID
- `assistant` (Body): Assistant object

**Response:**
- Success (204 No Content): Assistant created/updated
- Bad Request (400): Invalid input
- Unauthorized (401): Missing or invalid permissions

#### Delete Assistant
```http
DELETE /api/IAssistant/{id}
```

Deletes an assistant.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Assistant ID

**Response:**
- Success (204 No Content): Assistant deleted
- Bad Request (400): Invalid ID
- Unauthorized (401): Missing or invalid permissions

## Duoporta Controller

### Base URL
```
/api/Duoporta
```

### Endpoints

#### Get All Duoportas
```http
GET /api/Duoporta
```

Retrieves all duoportas.

**Authentication:** Requires API key or permission

**Response:**
- Success (200 OK): List of duoportas
- Unauthorized (401): Missing or invalid permissions

#### Get Duoporta by ID
```http
GET /api/Duoporta/{id}
```

Retrieves a specific duoporta.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Duoporta ID

**Response:**
- Success (200 OK): Duoporta object
- Bad Request (400): Invalid ID
- Not Found (404): Duoporta not found
- Unauthorized (401): Missing or invalid permissions

#### Create/Update Duoporta
```http
PUT /api/Duoporta/{id}
```

Creates or updates a duoporta.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Duoporta ID
- `duoporta` (Body): Duoporta object

**Response:**
- Success (204 No Content): Duoporta created/updated
- Bad Request (400): Invalid input
- Unauthorized (401): Missing or invalid permissions

#### Delete Duoporta
```http
DELETE /api/Duoporta/{id}
```

Deletes a duoporta.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Duoporta ID

**Response:**
- Success (204 No Content): Duoporta deleted
- Bad Request (400): Invalid ID
- Unauthorized (401): Missing or invalid permissions

## Channel Controller

### Base URL
```
/api/Channel
```

### Endpoints

#### Get All Channels
```http
GET /api/Channel
```

Retrieves all channels.

**Authentication:** Requires API key or permission

**Response:**
- Success (200 OK): List of channels
- Unauthorized (401): Missing or invalid permissions

#### Get Channel by ID
```http
GET /api/Channel/{id}
```

Retrieves a specific channel.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Channel ID

**Response:**
- Success (200 OK): Channel object
- Bad Request (400): Invalid ID
- Not Found (404): Channel not found
- Unauthorized (401): Missing or invalid permissions

#### Create/Update Channel
```http
PUT /api/Channel/{id}
```

Creates or updates a channel.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Channel ID
- `channel` (Body): Channel object

**Response:**
- Success (204 No Content): Channel created/updated
- Bad Request (400): Invalid input
- Unauthorized (401): Missing or invalid permissions

#### Delete Channel
```http
DELETE /api/Channel/{id}
```

Deletes a channel.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Channel ID

**Response:**
- Success (204 No Content): Channel deleted
- Bad Request (400): Invalid ID
- Unauthorized (401): Missing or invalid permissions

## Walter1 Assistants Controller

### Base URL
```
/api/Walter1Assistants
```

### Endpoints

#### Get All Assistants
```http
GET /api/Walter1Assistants
```

Retrieves all assistants.

**Authentication:** Requires API key or permission

**Response:**
- Success (200 OK): List of assistants
- Unauthorized (401): Missing or invalid permissions

#### Get Assistant by ID
```http
GET /api/Walter1Assistants/{id}
```

Retrieves a specific assistant.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Assistant ID

**Response:**
- Success (200 OK): Assistant object
- Bad Request (400): Invalid ID
- Not Found (404): Assistant not found
- Unauthorized (401): Missing or invalid permissions

#### Create/Update Assistant
```http
PUT /api/Walter1Assistants/{id}
```

Creates or updates an assistant.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Assistant ID
- `assistant` (Body): Assistant object

**Response:**
- Success (204 No Content): Assistant created/updated
- Bad Request (400): Invalid input
- Unauthorized (401): Missing or invalid permissions

#### Delete Assistant
```http
DELETE /api/Walter1Assistants/{id}
```

Deletes an assistant.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Assistant ID

**Response:**
- Success (204 No Content): Assistant deleted
- Bad Request (400): Invalid ID
- Unauthorized (401): Missing or invalid permissions

## Text to Schema Controller

### Base URL
```
/api/TextToSchema
```

### Endpoints

#### Convert Text to Schema
```http
POST /api/TextToSchema/convert
```

Converts text to a schema.

**Request Parameters:**
- `text` (Form Data): Text to convert

**Response:**
- Success (200 OK): Schema object
- Bad Request (400): Invalid input or processing error
- Unauthorized (401): Missing or invalid permissions

**Headers:**
- `Content-Disposition`: Attachment with filename
- `Content-Type`: application/json

## PostgreSQL Controller

### Base URL
```
/api/PostgreSQL
```

### Endpoints

#### Get All Databases
```http
GET /api/PostgreSQL
```

Retrieves all databases.

**Authentication:** Requires API key or permission

**Response:**
- Success (200 OK): List of databases
- Unauthorized (401): Missing or invalid permissions

#### Get Database by ID
```http
GET /api/PostgreSQL/{id}
```

Retrieves a specific database.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Database ID

**Response:**
- Success (200 OK): Database object
- Bad Request (400): Invalid ID
- Not Found (404): Database not found
- Unauthorized (401): Missing or invalid permissions

#### Create/Update Database
```http
PUT /api/PostgreSQL/{id}
```

Creates or updates a database.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Database ID
- `database` (Body): Database object

**Response:**
- Success (204 No Content): Database created/updated
- Bad Request (400): Invalid input
- Unauthorized (401): Missing or invalid permissions

#### Delete Database
```http
DELETE /api/PostgreSQL/{id}
```

Deletes a database.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Database ID

**Response:**
- Success (204 No Content): Database deleted
- Bad Request (400): Invalid ID
- Unauthorized (401): Missing or invalid permissions

## PDF Controller

### Base URL
```
/api/PDF
```

### Endpoints

#### Get All PDFs
```http
GET /api/PDF
```

Retrieves all PDFs.

**Authentication:** Requires API key or permission

**Response:**
- Success (200 OK): List of PDFs
- Unauthorized (401): Missing or invalid permissions

#### Get PDF by ID
```http
GET /api/PDF/{id}
```

Retrieves a specific PDF.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): PDF ID

**Response:**
- Success (200 OK): PDF object
- Bad Request (400): Invalid ID
- Not Found (404): PDF not found
- Unauthorized (401): Missing or invalid permissions

#### Create/Update PDF
```http
PUT /api/PDF/{id}
```

Creates or updates a PDF.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): PDF ID
- `pdf` (Body): PDF object

**Response:**
- Success (204 No Content): PDF created/updated
- Bad Request (400): Invalid input
- Unauthorized (401): Missing or invalid permissions

#### Delete PDF
```http
DELETE /api/PDF/{id}
```

Deletes a PDF.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): PDF ID

**Response:**
- Success (204 No Content): PDF deleted
- Bad Request (400): Invalid ID
- Unauthorized (401): Missing or invalid permissions

## Lua Controller

### Base URL
```
/api/Lua
```

### Endpoints

#### Get All Lua Scripts
```http
GET /api/Lua
```

Retrieves all Lua scripts.

**Authentication:** Requires API key or permission

**Response:**
- Success (200 OK): List of Lua scripts
- Unauthorized (401): Missing or invalid permissions

#### Get Lua Script by ID
```http
GET /api/Lua/{id}
```

Retrieves a specific Lua script.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Lua script ID

**Response:**
- Success (200 OK): Lua script object
- Bad Request (400): Invalid ID
- Not Found (404): Lua script not found
- Unauthorized (401): Missing or invalid permissions

#### Create/Update Lua Script
```http
PUT /api/Lua/{id}
```

Creates or updates a Lua script.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Lua script ID
- `script` (Body): Lua script object

**Response:**
- Success (204 No Content): Lua script created/updated
- Bad Request (400): Invalid input
- Unauthorized (401): Missing or invalid permissions

#### Delete Lua Script
```http
DELETE /api/Lua/{id}
```

Deletes a Lua script.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Lua script ID

**Response:**
- Success (204 No Content): Lua script deleted
- Bad Request (400): Invalid ID
- Unauthorized (401): Missing or invalid permissions

## JSON Table Controller

### Base URL
```
/api/JSONTable
```

### Endpoints

#### Get All JSON Tables
```http
GET /api/JSONTable
```

Retrieves all JSON tables.

**Authentication:** Requires API key or permission

**Response:**
- Success (200 OK): List of JSON tables
- Unauthorized (401): Missing or invalid permissions

#### Get JSON Table by ID
```http
GET /api/JSONTable/{id}
```

Retrieves a specific JSON table.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): JSON table ID

**Response:**
- Success (200 OK): JSON table object
- Bad Request (400): Invalid ID
- Not Found (404): JSON table not found
- Unauthorized (401): Missing or invalid permissions

#### Create/Update JSON Table
```http
PUT /api/JSONTable/{id}
```

Creates or updates a JSON table.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): JSON table ID
- `table` (Body): JSON table object

**Response:**
- Success (204 No Content): JSON table created/updated
- Bad Request (400): Invalid input
- Unauthorized (401): Missing or invalid permissions

#### Delete JSON Table
```http
DELETE /api/JSONTable/{id}
```

Deletes a JSON table.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): JSON table ID

**Response:**
- Success (204 No Content): JSON table deleted
- Bad Request (400): Invalid ID
- Unauthorized (401): Missing or invalid permissions

## GPT Vision Extractor Controller

### Base URL
```
/api/GPTVisionExtractor
```

### Endpoints

#### Extract Vision Features
```http
POST /api/GPTVisionExtractor/extract
```

Extracts vision features from an image.

**Request Parameters:**
- `file` (Form Data): Image file to extract features from (.jpg, .jpeg, .png, .gif, .bmp)

**Response:**
- Success (200 OK): Vision features extracted successfully
- Bad Request (400): Invalid file format or empty file
- Internal Server Error (500): Processing error

**Headers:**
- `Content-Disposition`: Attachment with filename
- `Content-Type`: application/json

## Flat Query Assistant Controller

### Base URL
```
/api/FlatQueryAssistant
```

### Endpoints

#### Get All Assistants
```http
GET /api/FlatQueryAssistant
```

Retrieves all assistants.

**Authentication:** Requires API key or permission

**Response:**
- Success (200 OK): List of assistants
- Unauthorized (401): Missing or invalid permissions

#### Get Assistant by ID
```http
GET /api/FlatQueryAssistant/{id}
```

Retrieves a specific assistant.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Assistant ID

**Response:**
- Success (200 OK): Assistant object
- Bad Request (400): Invalid ID
- Not Found (404): Assistant not found
- Unauthorized (401): Missing or invalid permissions

#### Create/Update Assistant
```http
PUT /api/FlatQueryAssistant/{id}
```

Creates or updates an assistant.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Assistant ID
- `assistant` (Body): Assistant object

**Response:**
- Success (204 No Content): Assistant created/updated
- Bad Request (400): Invalid input
- Unauthorized (401): Missing or invalid permissions

#### Delete Assistant
```http
DELETE /api/FlatQueryAssistant/{id}
```

Deletes an assistant.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Assistant ID

**Response:**
- Success (204 No Content): Assistant deleted
- Bad Request (400): Invalid ID
- Unauthorized (401): Missing or invalid permissions

## DocX to PDF Controller

### Base URL
```
/api/DocXToPDF
```

### Endpoints

#### Convert DocX to PDF
```http
POST /api/DocXToPDF/convert
```

Converts a DocX file to PDF format.

**Request Parameters:**
- `file` (Form Data): DocX file to convert (.docx)

**Response:**
- Success (200 OK): PDF file
- Bad Request (400): Invalid file format or empty file
- Internal Server Error (500): Processing error

**Headers:**
- `Content-Disposition`: Attachment with filename
- `Content-Type`: application/pdf

#### Get All DocX Files
```http
GET /api/DocXToPDF
```

Retrieves all DocX files.

**Authentication:** Requires API key or permission

**Response:**
- Success (200 OK): List of DocX files
- Unauthorized (401): Missing or invalid permissions

#### Get DocX File by ID
```http
GET /api/DocXToPDF/{id}
```

Retrieves a specific DocX file.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): DocX file ID

**Response:**
- Success (200 OK): DocX file object
- Bad Request (400): Invalid ID
- Not Found (404): DocX file not found
- Unauthorized (401): Missing or invalid permissions

#### Create/Update DocX File
```http
PUT /api/DocXToPDF/{id}
```

Creates or updates a DocX file.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): DocX file ID
- `file` (Body): DocX file object

**Response:**
- Success (204 No Content): DocX file created/updated
- Bad Request (400): Invalid input
- Unauthorized (401): Missing or invalid permissions

#### Delete DocX File
```http
DELETE /api/DocXToPDF/{id}
```

Deletes a DocX file.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): DocX file ID

**Response:**
- Success (204 No Content): DocX file deleted
- Bad Request (400): Invalid ID
- Unauthorized (401): Missing or invalid permissions

## Call Analyzer Controller

### Base URL
```
/api/CallAnalyzer
```

### Endpoints

#### Analyze Call
```http
POST /api/CallAnalyzer/analyze
```

Analyzes a call recording or transcript.

**Authentication:** Requires API key or permission

**Request Parameters:**
- `file` (Form Data): Call recording or transcript file
- `metadata` (Form Data, optional): Additional metadata about the call

**Response:**
- Success (200 OK): Analysis results
- Bad Request (400): Invalid input or processing error
- Unauthorized (401): Missing or invalid permissions

**Headers:**
- `Content-Type`: application/json

## Walter1 Assistants Config Controller

### Base URL
```
/api/config/walter1-assistants
```

### Endpoints

#### Get All Configurations
```http
GET /api/config/walter1-assistants
```

Retrieves all Walter1 assistants configurations.

**Authentication:** Requires API key or permission

**Response:**
- Success (200 OK): List of configurations
- Unauthorized (401): Missing or invalid permissions

#### Get Configuration by ID
```http
GET /api/config/walter1-assistants/{id}
```

Retrieves a specific Walter1 assistants configuration.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Configuration ID

**Response:**
- Success (200 OK): Configuration object
- Bad Request (400): Invalid ID
- Not Found (404): Configuration not found
- Unauthorized (401): Missing or invalid permissions

#### Create/Update Configuration
```http
PUT /api/config/walter1-assistants/{id}
```

Creates or updates a Walter1 assistants configuration.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Configuration ID
- `config` (Body): Configuration object

**Response:**
- Success (204 No Content): Configuration created/updated
- Bad Request (400): Invalid input
- Unauthorized (401): Missing or invalid permissions

#### Delete Configuration
```http
DELETE /api/config/walter1-assistants/{id}
```

Deletes a Walter1 assistants configuration.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Configuration ID

**Response:**
- Success (204 No Content): Configuration deleted
- Bad Request (400): Invalid ID
- Unauthorized (401): Missing or invalid permissions

## VectorMind Live Assistants Config Controller

### Base URL
```
/api/config/vectormind-live-assistants
```

### Endpoints

#### Get All Configurations
```http
GET /api/config/vectormind-live-assistants
```

Retrieves all VectorMind live assistants configurations.

**Authentication:** Requires API key or permission

**Response:**
- Success (200 OK): List of configurations
- Unauthorized (401): Missing or invalid permissions

#### Get Configuration by ID
```http
GET /api/config/vectormind-live-assistants/{id}
```

Retrieves a specific VectorMind live assistants configuration.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Configuration ID

**Response:**
- Success (200 OK): Configuration object
- Bad Request (400): Invalid ID
- Not Found (404): Configuration not found
- Unauthorized (401): Missing or invalid permissions

#### Create/Update Configuration
```http
PUT /api/config/vectormind-live-assistants/{id}
```

Creates or updates a VectorMind live assistants configuration.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Configuration ID
- `config` (Body): Configuration object

**Response:**
- Success (204 No Content): Configuration created/updated
- Bad Request (400): Invalid input
- Unauthorized (401): Missing or invalid permissions

#### Delete Configuration
```http
DELETE /api/config/vectormind-live-assistants/{id}
```

Deletes a VectorMind live assistants configuration.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Configuration ID

**Response:**
- Success (204 No Content): Configuration deleted
- Bad Request (400): Invalid ID
- Unauthorized (401): Missing or invalid permissions

## Text to Schema Assistants Config Controller

### Base URL
```
/api/config/text-to-schema-assistants
```

### Endpoints

#### Get All Configurations
```http
GET /api/config/text-to-schema-assistants
```

Retrieves all text to schema assistants configurations.

**Authentication:** Requires API key or permission

**Response:**
- Success (200 OK): List of configurations
- Unauthorized (401): Missing or invalid permissions

#### Get Configuration by ID
```http
GET /api/config/text-to-schema-assistants/{id}
```

Retrieves a specific text to schema assistants configuration.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Configuration ID

**Response:**
- Success (200 OK): Configuration object
- Bad Request (400): Invalid ID
- Not Found (404): Configuration not found
- Unauthorized (401): Missing or invalid permissions

#### Create/Update Configuration
```http
PUT /api/config/text-to-schema-assistants/{id}
```

Creates or updates a text to schema assistants configuration.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Configuration ID
- `config` (Body): Configuration object

**Response:**
- Success (204 No Content): Configuration created/updated
- Bad Request (400): Invalid input
- Unauthorized (401): Missing or invalid permissions

#### Delete Configuration
```http
DELETE /api/config/text-to-schema-assistants/{id}
```

Deletes a text to schema assistants configuration.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Configuration ID

**Response:**
- Success (204 No Content): Configuration deleted
- Bad Request (400): Invalid ID
- Unauthorized (401): Missing or invalid permissions

## Scheduled Question Agents Config Controller

### Base URL
```
/api/config/scheduled-question-agents
```

### Endpoints

#### Get All Configurations
```http
GET /api/config/scheduled-question-agents
```

Retrieves all scheduled question agents configurations.

**Authentication:** Requires API key or permission

**Response:**
- Success (200 OK): List of configurations
- Unauthorized (401): Missing or invalid permissions

#### Get Configuration by ID
```http
GET /api/config/scheduled-question-agents/{id}
```

Retrieves a specific scheduled question agents configuration.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Configuration ID

**Response:**
- Success (200 OK): Configuration object
- Bad Request (400): Invalid ID
- Not Found (404): Configuration not found
- Unauthorized (401): Missing or invalid permissions

#### Create/Update Configuration
```http
PUT /api/config/scheduled-question-agents/{id}
```

Creates or updates a scheduled question agents configuration.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Configuration ID
- `config` (Body): Configuration object

**Response:**
- Success (204 No Content): Configuration created/updated
- Bad Request (400): Invalid input
- Unauthorized (401): Missing or invalid permissions

#### Delete Configuration
```http
DELETE /api/config/scheduled-question-agents/{id}
```

Deletes a scheduled question agents configuration.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Configuration ID

**Response:**
- Success (204 No Content): Configuration deleted
- Bad Request (400): Invalid ID
- Unauthorized (401): Missing or invalid permissions

## PostgreSQL Clients Config Controller

### Base URL
```
/api/config/postgresql-clients
```

### Endpoints

#### Get All Configurations
```http
GET /api/config/postgresql-clients
```

Retrieves all PostgreSQL clients configurations.

**Authentication:** Requires API key or permission

**Response:**
- Success (200 OK): List of configurations
- Unauthorized (401): Missing or invalid permissions

#### Get Configuration by ID
```http
GET /api/config/postgresql-clients/{id}
```

Retrieves a specific PostgreSQL clients configuration.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Configuration ID

**Response:**
- Success (200 OK): Configuration object
- Bad Request (400): Invalid ID
- Not Found (404): Configuration not found
- Unauthorized (401): Missing or invalid permissions

#### Create/Update Configuration
```http
PUT /api/config/postgresql-clients/{id}
```

Creates or updates a PostgreSQL clients configuration.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Configuration ID
- `config` (Body): Configuration object

**Response:**
- Success (204 No Content): Configuration created/updated
- Bad Request (400): Invalid input
- Unauthorized (401): Missing or invalid permissions

#### Delete Configuration
```http
DELETE /api/config/postgresql-clients/{id}
```

Deletes a PostgreSQL clients configuration.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Configuration ID

**Response:**
- Success (204 No Content): Configuration deleted
- Bad Request (400): Invalid ID
- Unauthorized (401): Missing or invalid permissions

## Lua Core Config Controller

### Base URL
```
/api/config/lua-core
```

### Endpoints

#### Get All Configurations
```http
GET /api/config/lua-core
```

Retrieves all Lua core configurations.

**Authentication:** Requires API key or permission

**Response:**
- Success (200 OK): List of configurations
- Unauthorized (401): Missing or invalid permissions

#### Get Configuration by ID
```http
GET /api/config/lua-core/{id}
```

Retrieves a specific Lua core configuration.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Configuration ID

**Response:**
- Success (200 OK): Configuration object
- Bad Request (400): Invalid ID
- Not Found (404): Configuration not found
- Unauthorized (401): Missing or invalid permissions

#### Create/Update Configuration
```http
PUT /api/config/lua-core/{id}
```

Creates or updates a Lua core configuration.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Configuration ID
- `config` (Body): Configuration object

**Response:**
- Success (204 No Content): Configuration created/updated
- Bad Request (400): Invalid input
- Unauthorized (401): Missing or invalid permissions

#### Delete Configuration
```http
DELETE /api/config/lua-core/{id}
```

Deletes a Lua core configuration.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Configuration ID

**Response:**
- Success (204 No Content): Configuration deleted
- Bad Request (400): Invalid ID
- Unauthorized (401): Missing or invalid permissions

## JSON Schemas Config Controller

### Base URL
```
/api/config/json-schemas
```

### Endpoints

#### Get All Configurations
```http
GET /api/config/json-schemas
```

Retrieves all JSON schemas configurations.

**Authentication:** Requires API key or permission

**Response:**
- Success (200 OK): List of configurations
- Unauthorized (401): Missing or invalid permissions

#### Get Configuration by ID
```http
GET /api/config/json-schemas/{id}
```

Retrieves a specific JSON schemas configuration.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Configuration ID

**Response:**
- Success (200 OK): Configuration object
- Bad Request (400): Invalid ID
- Not Found (404): Configuration not found
- Unauthorized (401): Missing or invalid permissions

#### Create/Update Configuration
```http
PUT /api/config/json-schemas/{id}
```

Creates or updates a JSON schemas configuration.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Configuration ID
- `config` (Body): Configuration object

**Response:**
- Success (204 No Content): Configuration created/updated
- Bad Request (400): Invalid input
- Unauthorized (401): Missing or invalid permissions

#### Delete Configuration
```http
DELETE /api/config/json-schemas/{id}
```

Deletes a JSON schemas configuration.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Configuration ID

**Response:**
- Success (204 No Content): Configuration deleted
- Bad Request (400): Invalid ID
- Unauthorized (401): Missing or invalid permissions

## Flat Query Assistants Config Controller

### Base URL
```
/api/config/flat-query-assistants
```

### Endpoints

#### Get All Configurations
```http
GET /api/config/flat-query-assistants
```

Retrieves all flat query assistants configurations.

**Authentication:** Requires API key or permission

**Response:**
- Success (200 OK): List of configurations
- Unauthorized (401): Missing or invalid permissions

#### Get Configuration by ID
```http
GET /api/config/flat-query-assistants/{id}
```

Retrieves a specific flat query assistants configuration.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Configuration ID

**Response:**
- Success (200 OK): Configuration object
- Bad Request (400): Invalid ID
- Not Found (404): Configuration not found
- Unauthorized (401): Missing or invalid permissions

#### Create/Update Configuration
```http
PUT /api/config/flat-query-assistants/{id}
```

Creates or updates a flat query assistants configuration.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Configuration ID
- `config` (Body): Configuration object

**Response:**
- Success (204 No Content): Configuration created/updated
- Bad Request (400): Invalid input
- Unauthorized (401): Missing or invalid permissions

#### Delete Configuration
```http
DELETE /api/config/flat-query-assistants/{id}
```

Deletes a flat query assistants configuration.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Configuration ID

**Response:**
- Success (204 No Content): Configuration deleted
- Bad Request (400): Invalid ID
- Unauthorized (401): Missing or invalid permissions

## Pinecones Config Controller

### Base URL
```
/api/config/pinecones
```

### Endpoints

#### Get All Configurations
```http
GET /api/config/pinecones
```

Retrieves all Pinecones configurations.

**Authentication:** Requires API key or permission

**Response:**
- Success (200 OK): List of configurations
- Unauthorized (401): Missing or invalid permissions

#### Get Configuration by ID
```http
GET /api/config/pinecones/{id}
```

Retrieves a specific Pinecones configuration.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Configuration ID

**Response:**
- Success (200 OK): Configuration object
- Bad Request (400): Invalid ID
- Not Found (404): Configuration not found
- Unauthorized (401): Missing or invalid permissions

#### Create/Update Configuration
```http
PUT /api/config/pinecones/{id}
```

Creates or updates a Pinecones configuration.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Configuration ID
- `config` (Body): Configuration object

**Response:**
- Success (204 No Content): Configuration created/updated
- Bad Request (400): Invalid input
- Unauthorized (401): Missing or invalid permissions

#### Delete Configuration
```http
DELETE /api/config/pinecones/{id}
```

Deletes a Pinecones configuration.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Configuration ID

**Response:**
- Success (204 No Content): Configuration deleted
- Bad Request (400): Invalid ID
- Unauthorized (401): Missing or invalid permissions

## Pinecone Namespaces Config Controller

### Base URL
```
/api/config/pinecone-namespaces
```

### Endpoints

#### Get All Configurations
```http
GET /api/config/pinecone-namespaces
```

Retrieves all Pinecone namespaces configurations.

**Authentication:** Requires API key or permission

**Response:**
- Success (200 OK): List of configurations
- Unauthorized (401): Missing or invalid permissions

#### Get Configuration by ID
```http
GET /api/config/pinecone-namespaces/{id}
```

Retrieves a specific Pinecone namespaces configuration.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Configuration ID

**Response:**
- Success (200 OK): Configuration object
- Bad Request (400): Invalid ID
- Not Found (404): Configuration not found
- Unauthorized (401): Missing or invalid permissions

#### Create/Update Configuration
```http
PUT /api/config/pinecone-namespaces/{id}
```

Creates or updates a Pinecone namespaces configuration.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Configuration ID
- `config` (Body): Configuration object

**Response:**
- Success (204 No Content): Configuration created/updated
- Bad Request (400): Invalid input
- Unauthorized (401): Missing or invalid permissions

#### Delete Configuration
```http
DELETE /api/config/pinecone-namespaces/{id}
```

Deletes a Pinecone namespaces configuration.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Configuration ID

**Response:**
- Success (204 No Content): Configuration deleted
- Bad Request (400): Invalid ID
- Unauthorized (401): Missing or invalid permissions

## Channels Config Controller

### Base URL
```
/api/config/channels
```

### Endpoints

#### Get All Configurations
```http
GET /api/config/channels
```

Retrieves all channels configurations.

**Authentication:** Requires API key or permission

**Response:**
- Success (200 OK): List of configurations
- Unauthorized (401): Missing or invalid permissions

#### Get Configuration by ID
```http
GET /api/config/channels/{id}
```

Retrieves a specific channels configuration.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Configuration ID

**Response:**
- Success (200 OK): Configuration object
- Bad Request (400): Invalid ID
- Not Found (404): Configuration not found
- Unauthorized (401): Missing or invalid permissions

#### Create/Update Configuration
```http
PUT /api/config/channels/{id}
```

Creates or updates a channels configuration.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Configuration ID
- `config` (Body): Configuration object

**Response:**
- Success (204 No Content): Configuration created/updated
- Bad Request (400): Invalid input
- Unauthorized (401): Missing or invalid permissions

#### Delete Configuration
```http
DELETE /api/config/channels/{id}
```

Deletes a channels configuration.

**Authentication:** Requires API key or permission

**Parameters:**
- `id` (Path): Configuration ID

**Response:**
- Success (204 No Content): Configuration deleted
- Bad Request (400): Invalid ID
- Unauthorized (401): Missing or invalid permissions 