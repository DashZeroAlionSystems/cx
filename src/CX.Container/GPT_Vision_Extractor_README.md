# GPT Vision Extractor API

## Overview

The GPT Vision Extractor API leverages GPT-4 Vision AI to extract text from images and PDF files. This powerful service can process various image formats and PDF documents, returning both formatted and raw extracted text.

## Features

- **Multi-format Support**: Supports JPG, PNG, GIF, WebP, and PDF files
- **AI-Powered Extraction**: Uses GPT-4 Vision for accurate text recognition
- **Metadata Support**: Optional document metadata for enhanced processing
- **Dual Output**: Returns both formatted and raw extracted text
- **Processing Analytics**: Includes processing time information

## API Endpoints

### Extract Text from Image/PDF

**Endpoint:** `POST /api/Gpt4VisionExtractor/extract`

**Description:** Extracts text from uploaded image or PDF files using GPT-4 Vision AI.

## Authentication

All requests require a Bearer token in the Authorization header:

```
Authorization: Bearer your-access-token
```

## Request Format

The API accepts `multipart/form-data` requests with the following parameters:

### Required Parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `file` | File | Image or PDF file to extract text from |

### Optional Metadata Parameters

| Parameter | Type | Description | Example |
|-----------|------|-------------|---------|
| `metadata.Id` | String | Document identifier | `doc-123` |
| `metadata.Pages` | Integer | Number of pages | `5` |
| `metadata.ContainsTables` | Boolean | Whether document contains tables | `true` |
| `metadata.Description` | String | Document description | `Invoice document` |
| `metadata.SourceDocument` | String | Original document name | `invoice.pdf` |
| `metadata.Organization` | String | Organization name | `Acme Corp` |

## Supported File Formats

- **Images**: JPG, JPEG, PNG, GIF, WebP
- **Documents**: PDF

## Response Format

### Success Response (200 OK)

```json
{
    "extractedText": "This is the formatted extracted text from the document...",
    "rawText": "This is the raw extracted text without formatting...",
    "fileType": ".pdf",
    "processingTime": "2024-01-15T10:30:00Z"
}
```

### Response Fields

| Field | Type | Description |
|-------|------|-------------|
| `extractedText` | String | Formatted extracted text with structure preserved |
| `rawText` | String | Raw extracted text without formatting |
| `fileType` | String | File extension of the processed file |
| `processingTime` | String | ISO 8601 timestamp of processing completion |

## Usage Examples

### Using cURL

```bash
curl -X POST "https://localhost:7001/api/Gpt4VisionExtractor/extract" \
  -H "Authorization: Bearer your-access-token" \
  -F "file=@document.pdf" \
  -F "metadata.Description=Sample invoice" \
  -F "metadata.Organization=Test Company"
```

### Using Postman

1. Import the `GPT_Vision_Extractor_Collection.postman_collection.json` file
2. Set the `base_url` variable to your API endpoint
3. Set the `access_token` variable to your bearer token
4. Select a file in the request body
5. Optionally configure metadata parameters
6. Send the request

## Error Handling

The API returns appropriate HTTP status codes:

- `200 OK`: Successful text extraction
- `400 Bad Request`: Invalid file format or missing required parameters
- `401 Unauthorized`: Invalid or missing authentication token
- `413 Payload Too Large`: File size exceeds limits
- `422 Unprocessable Entity`: File could not be processed
- `500 Internal Server Error`: Server-side processing error

## Best Practices

1. **File Size**: Keep files under reasonable size limits for optimal performance
2. **Authentication**: Always include valid bearer tokens
3. **File Formats**: Use supported formats for best results
4. **Metadata**: Include relevant metadata to improve extraction accuracy
5. **Error Handling**: Implement proper error handling for production use

## Rate Limits

Please refer to your API subscription plan for rate limiting information.

## Support

For technical support or questions about the GPT Vision Extractor API, please contact the development team. 