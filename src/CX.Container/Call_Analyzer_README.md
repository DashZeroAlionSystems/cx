# Call Analyzer API

## Overview

The Call Analyzer API provides advanced audio call analysis capabilities. It processes audio files to extract valuable insights including sentiment analysis, keyword extraction, duration tracking, and full transcription services.

## Features

- **Multi-format Audio Support**: Supports WAV, MP3, OGG, and M4A formats
- **Sentiment Analysis**: Determines the emotional tone of conversations
- **Keyword Extraction**: Identifies important keywords and phrases
- **Transcription**: Converts speech to text
- **Duration Analysis**: Provides call duration metrics
- **Configurable Analyzers**: Support for different analyzer configurations

## API Endpoints

### Analyze Audio Call

**Endpoint:** `POST /api/call-analyzer/{analyzer_id}`

**Description:** Analyzes audio call files using a specified call analyzer configuration.

## Authentication

All requests require a Bearer token in the Authorization header:

```
Authorization: Bearer your-access-token
```

## Request Format

The API accepts `multipart/form-data` requests with the following parameters:

### URL Parameters

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `analyzer_id` | String | Yes | ID of the call analyzer to use |

### Request Body

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `file` | File | Yes | Audio file to analyze (WAV, MP3, OGG, M4A) |

## Supported Audio Formats

- **WAV**: Uncompressed audio format
- **MP3**: Compressed audio format
- **OGG**: Open-source compressed audio format
- **M4A**: MPEG-4 audio format

## Response Format

### Success Response (200 OK)

```json
{
    "analysisResults": {
        "sentiment": "positive",
        "keywords": ["customer", "service", "satisfaction"],
        "duration": 180,
        "transcription": "Hello, how can I help you today? I'm calling about my recent order..."
    }
}
```

### Response Fields

| Field | Type | Description |
|-------|------|-------------|
| `analysisResults` | Object | Container for all analysis results |
| `sentiment` | String | Overall sentiment of the call (positive, negative, neutral) |
| `keywords` | Array | List of important keywords extracted from the conversation |
| `duration` | Integer | Call duration in seconds |
| `transcription` | String | Full text transcription of the audio |

## Available Analyzers

Different analyzer configurations may provide different analysis capabilities:

- `default-analyzer`: Standard analysis with sentiment, keywords, and transcription
- `customer-service`: Specialized for customer service call analysis
- `sales-call`: Optimized for sales conversation analysis
- `support-ticket`: Focused on technical support interactions

## Usage Examples

### Using cURL

```bash
curl -X POST "https://localhost:7001/api/call-analyzer/default-analyzer" \
  -H "Authorization: Bearer your-access-token" \
  -F "file=@call-recording.wav"
```

### Using Postman

1. Import the `Call_Analyzer_Collection.postman_collection.json` file
2. Set the `base_url` variable to your API endpoint
3. Set the `access_token` variable to your bearer token
4. Set the `analyzer_id` variable to your desired analyzer
5. Select an audio file in the request body
6. Send the request

## Error Handling

The API returns appropriate HTTP status codes:

- `200 OK`: Successful analysis completed
- `400 Bad Request`: Invalid audio format or missing parameters
- `401 Unauthorized`: Invalid or missing authentication token
- `404 Not Found`: Analyzer ID not found
- `413 Payload Too Large`: Audio file size exceeds limits
- `422 Unprocessable Entity`: Audio file could not be processed
- `500 Internal Server Error`: Server-side processing error

## Audio File Requirements

### File Size Limits
- Maximum file size: Refer to your API plan limits
- Recommended: Keep files under 100MB for optimal performance

### Audio Quality
- Sample rate: 8kHz minimum, 44.1kHz recommended
- Bit depth: 16-bit minimum
- Channels: Mono or stereo supported
- Duration: Up to 60 minutes per file

## Best Practices

1. **Audio Quality**: Use high-quality recordings for better analysis accuracy
2. **File Formats**: WAV format typically provides the best results
3. **Analyzer Selection**: Choose the appropriate analyzer for your use case
4. **Background Noise**: Minimize background noise for improved transcription
5. **Speaker Clarity**: Ensure clear speech for optimal keyword extraction
6. **File Size**: Compress large files while maintaining quality

## Privacy and Security

- All audio files are processed securely
- Files are not stored permanently after processing
- Transcriptions and analysis data follow data retention policies
- Ensure compliance with local privacy regulations

## Rate Limits

Please refer to your API subscription plan for rate limiting information.

## Troubleshooting

### Common Issues

1. **Poor Transcription Quality**
   - Check audio quality and clarity
   - Reduce background noise
   - Use supported audio formats

2. **Analysis Timeout**
   - Reduce file size
   - Check network connectivity
   - Verify analyzer availability

3. **Incorrect Sentiment Analysis**
   - Ensure clear speech patterns
   - Consider context-specific analyzers
   - Review keyword extraction results

## Support

For technical support or questions about the Call Analyzer API, please contact the development team. 