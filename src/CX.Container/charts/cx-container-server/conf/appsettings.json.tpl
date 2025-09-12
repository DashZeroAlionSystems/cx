{
  "Logging": {
    "LogLevel": {{- toPrettyJson .Values.Config.LogLevelOptions | indent 4 | trim }}
  },
  "ConnectionStrings": {
    "default": {{ .Values.Config.Data.ConnectionString | quote }}
  },
  "AuthOptions": {
    "Audience": {{ .Values.Config.OAuth.Audience | quote }},
    "Authority": {{ .Values.Config.OAuth.Authority | quote }}    
  },
  "AwsSystemOptions": {
    "PublicBucket": {{ .Values.Config.BucketStorage.PublicBucketName | quote }},
    "PrivateBucket": {{ .Values.Config.BucketStorage.PrivateBucketName | quote }},
    "Region": {{ .Values.Config.BucketStorage.AwsRegion | quote }},
    "AccessKeyId": {{ .Values.Config.BucketStorage.AwsAccessKeyId | quote }},
    "SecretAccessKey": {{ .Values.Config.BucketStorage.AwsAccessKey | quote }},
    "Session": {{ .Values.Config.BucketStorage.AwsSession | quote }}
  },
  "AiOptions": {
    "UseVectorLinkImporter": {{ .Values.Config.AiServer.UseVectorLinkImporter }},
    "UseVectorLinkDocumentExtractors": {{ .Values.Config.AiServer.UseVectorLinkDocumentExtractors }},
    "ChannelName": {{ .Values.Config.AiServer.ChannelName | quote }},
    "ChatUrl": {{ .Values.Config.AiServer.Chat.Url | quote }},
    "ChatApiKey": {{ .Values.Config.AiServer.Chat.ApiKey | quote }},
    "TrainApiKey": {{ .Values.Config.AiServer.Train.ApiKey | quote }},
    "TrainUrl": {{ .Values.Config.AiServer.Train.Url | quote }},
    "DocumentUrl": {{ .Values.Config.AiServer.Document.Url | quote }},
    "DocumentApiKey": {{ .Values.Config.AiServer.Document.ApiKey | quote }},
    "HttpTimeoutSeconds": {{ .Values.Config.AiServer.HttpTimeoutSeconds }},
    "AutoProcess": {{ .Values.Config.AiServer.AutoProcess }},
    "ProcessDocumentUrl": {{ .Values.Config.AiServer.Document.ProcessDocumentUrl | quote }},
    "ProcessDocumentStatusUrl": {{ .Values.Config.AiServer.Document.ProcessDocumentStatusUrl | quote }},
    "DocumentKey": {{ .Values.Config.AiServer.Document.ApiKey | quote }},
    "DecoratorUrl": {{ .Values.Config.AiServer.DecoratorUrl | quote }},
    "DecoratorStatusUrl": {{ .Values.Config.AiServer.DecoratorStatusUrl | quote }},
    "DeleteUrl": {{ .Values.Config.AiServer.DeleteUrl | quote }},
    "TrainNamespace": {{ .Values.Config.AiServer.Train.Namespace | quote }},
    "TrainIndexName": {{ .Values.Config.AiServer.Train.IndexName | quote }}
  },
  "OpenAiOptions": {
    "ApiKey": {{ .Values.Config.OpenAiServer.ApiKey | quote }},
    "ApiUrl": {{ .Values.Config.OpenAiServer.ApiUrl | quote }},
    "ModelName": {{ .Values.Config.OpenAiServer.ModelName | quote }},
    "PromptKey": {{ .Values.Config.OpenAiServer.PromptKey | quote }},
    "PromptSubject": {{ .Values.Config.OpenAiServer.PromptSubject | quote }},
    "Prompt": {{ .Values.Config.OpenAiServer.Prompt | quote }}
  },
  "Pinecone": {
    "default": {
      "APIKey": {{ .Values.Config.Pinecone.ApiKey | quote }},
      "IndexName": {{ .Values.Config.Pinecone.IndexName | quote }},
      "EmbeddingModel": {{ .Values.Config.Pinecone.EmbeddingModel | quote }},
      "Namespace": {{ .Values.Config.Pinecone.Namespace | quote }},
      "MaxChunksPerPineconeQuery": 100,
      "MaxConcurrency": 25,
      "UseJsonVectorTracker": true,
      "JsonVectorTrackerName": "vector-tracker",
      "AttachmentsBaseUrl": {{ .Values.Config.Attachments.BaseUrl | quote }}
    }
  },
  "OpenAIEmbedder": {
    "APIKey": {{ .Values.Config.OpenAiServer.ApiKey | quote }},
    "MaxConcurrentCalls": 100
  },
  "OpenAIChatAgents": {
    "gpt-4o-mini": {
      "APIKey": {{ .Values.Config.OpenAiServer.ApiKey | quote }},
      "Model": "gpt-4o-mini",
      "MaxConcurrentCalls": 20
    },
    "gpt-4o": {
      "APIKey": {{ .Values.Config.OpenAiServer.ApiKey | quote }},
      "Model": "gpt-4o",
      "MaxConcurrentCalls": 12
    },
    "gpt-3.5-turbo": {
      "APIKey": {{ .Values.Config.OpenAiServer.ApiKey | quote }},
      "Model": "gpt-3.5-turbo",
      "MaxConcurrentCalls": 12
    },
    "o1-mini": {
      "APIKey": {{ .Values.Config.OpenAiServer.ApiKey | quote }},
      "Model": "o1-mini",
      "MaxConcurrentCalls": 12,
      "OnlyUserRole": true,
      "DefaultTemperature": 1
    },
    "o1-preview": {
      "APIKey": {{ .Values.Config.OpenAiServer.ApiKey | quote }},
      "Model": "o1-preview",
      "MaxConcurrentCalls": 12,
      "OnlyUserRole": true,
      "DefaultTemperature": 1
    },
    "business-gpt": {
      "APIKey": {{ .Values.Config.OpenAiServer.ApiKey | quote }}
    }
  },
  "LineSplitter": {
    "SegmentTokenLimit": {{ .Values.Config.LineSplitter.SegmentTokenLimit }}
  },
  "AzureAITranslators": {
    "en": {
      "ApiKey": {{ .Values.Config.AzureAITranslator.ApiKey | quote }},
      "Region": "swedencentral",
      "TargetLanguage": "en",
      "FailHard": true,
      "DontTranslateMinConfidence": 0.5,
      "RetryMaxDelaySeconds": 30,
      "RetryTimeoutSeconds": 180
    },
    "af": {
      "ApiKey": {{ .Values.Config.AzureAITranslator.ApiKey | quote }},
      "Region": "swedencentral",
      "TargetLanguage": "af",
      "FailHard": true,
      "DontTranslateMinConfidence": 0.5,
      "RetryMaxDelaySeconds": 30,
      "RetryTimeoutSeconds": 180
    },
    "nl": {
      "ApiKey": {{ .Values.Config.AzureAITranslator.ApiKey | quote }},
      "Region": "swedencentral",
      "TargetLanguage": "nl",
      "FailHard": true,
      "DontTranslateMinConfidence": 0.5,
      "RetryMaxDelaySeconds": 30,
      "RetryTimeoutSeconds": 180
    }
  },
  "AzureContentSafety": {
    "safe": {
      "ApiKey": {{ .Values.Config.AzureContentSafety.ApiKey | quote }},
      "Endpoint": {{ .Values.Config.AzureContentSafety.Endpoint | quote }},
      "FailHard": true,
      "RetryMaxDelaySeconds": 30,
      "RetryTimeoutSeconds": 180,
      "ExceptionHateLevel": 3,
      "ExceptionSexualLevel": 3,
      "ExceptionViolenceLevel": 4,
      "ExceptionSelfHarmLevel": 4
    }
  },
  "PostgreSQLClient": {
    "pg_default": {
      "ConnectionString": {{ .Values.Config.Data.ConnectionString | quote }},
      "MaxConcurrentQueries": 100
    }
  },
  "ContextAI": {
    "Host": {{ .Values.Config.ContextAI.Host | quote }},
    "Enabled": true,
    "ApiKey": {{ .Values.Config.ContextAI.ApiKey | quote }}
  },
  "Langfuse": {
    "BaseUrl": {{ .Values.Config.Langfuse.Host | quote }},
    "PublicKey": {{ .Values.Config.Langfuse.PublicKey | quote }},
    "SecretKey": {{ .Values.Config.Langfuse.SecretKey | quote }},
    "Enabled": true,
    "TraceImports": {{ .Values.Config.Langfuse.TraceImports | quote }}
  },
  "JsonStores": {
    "vector-tracker": {
      "PostgreSQLClientName": "pg_default",
      "TableName": "vector_tracker",
      "KeyLength": 36
    },
    "pdftojpg-documents": {
      "PostgreSQLClientName": "pg_default",
      "TableName": "pdftojpg_documents",
      "KeyLength": 50
    },
    "gpt4vision": {
      "PostgreSQLClientName": "pg_default",
      "TableName": "gpt4vision",
      "KeyLength": 100
    },
    "valid-url": {
      "PostgreSQLClientName": "pg_default",
      "TableName": "valid_url",
      "KeyLength": 300
    },
    "attachment_tracker": {
      "PostgreSQLClientName": "pg_default",
      "TableName": "attachment_tracker",
      "KeyLength": 36
    },
    "sakenetwerk_cache": {
      "PostgreSQLClientName": "pg_default",
      "TableName": "sakenetwerk_cache",
      "KeyLength": 255
    }
  },
  "PythonProcess": {
    "default": {
      "PythonInterpreterPath": "/usr/local/bin/python3.12",
      "WorkingDir": "/app/Py"
    }
  },
  "PythonDocX": {
    "ScriptPath": "docxtotext.py",
    "BinaryStore": "postgresql.docx_to_text",
    "PythonProcess": "default"
  },
  "PDFPlumber": {
    "ScriptPath": "pdfplumber_console.py",
    "BinaryStore": "postgresql.pdfplumber",
    "PythonProcess": "default"
  },
  "DocXToPDF": {
    "Enabled": true,
    "BinaryStore": "postgresql.docx_to_pdf"
  },
  "PDFToJpg": {
    "ScriptPath": "pdftojpg.py",
    "BinaryImageStore": "postgresql.pdf_to_jpg",
    "JsonDocumentStore": "pdftojpg-documents",
    "PythonProcess": "default",
    "PopplerPath": "/bin"
  },
  "PostgreSQLBinaryStores": {
    "pdf_to_jpg": {
      "PostgreSQLClientName": "pg_default",
      "TableName": "pdf_to_jpg",
      "KeyLength": 44
    },
    "docx_to_text": {
      "PostgreSQLClientName": "pg_default",
      "TableName": "docx_to_text",
      "KeyLength": 71
    },
    "pdfplumber": {
      "PostgreSQLClientName": "pg_default",
      "TableName": "pdf_to_text",
      "KeyLength": 71
    },
    "docx_to_pdf": {
      "PostgreSQLClientName": "pg_default",
      "TableName": "docx_to_pdf",
      "KeyLength": 71
    }
  },
  "Gpt4VisionExtractor": {
    "ChatAgent": "OpenAI.gpt-4o-mini",
    "SystemPrompt": {{ .Values.Config.Gpt4Vision.SystemPrompt | quote }},
    "Question": {{ .Values.Config.Gpt4Vision.Question | quote }},
    "JsonStore": "gpt4vision"
  },
  "VectorLinkImporter": {
    "ArchiveName": "pinecone.default",
    "AttachmentTrackerName": "attachment_tracker",
    "ProdRepoName": "default",
    "TrainCitations": {{ .Values.Config.VectorLinkImporter.TrainCitations | quote }},
    "PreferImageTextExtraction": {{ .Values.Config.VectorLinkImporter.PreferImageTextExtraction | quote }},
    "AttachToSelf": {{ .Values.Config.VectorLinkImporter.AttachToSelf | quote }},
    "DefaultAttachPageImages": {{ .Values.Config.VectorLinkImporter.DefaultAttachPageImages | quote }},
    "MaxConcurrency": {{ .Values.Config.VectorLinkImporter.MaxConcurrency }},
    "ExtractImages": {{ .Values.Config.VectorLinkImporter.ExtractImages | quote }},
    "DocumentProcessors": [
      {{ .Values.Config.VectorLinkImporter.TextProcessor1 | quote }}
    ]
  },
  "FileService": {
    "FileCacheDirectory": "/tmp"
  },
  "EmbeddingCache": {
    "UseCache": false
  },
  "ChatCache": {
    "UseCache": false
  },
  "Walter1Assistants": {
    "default": {
      "ChatAgent": {{ .Values.Config.Walter1Assistant.ChatAgent | quote }},
      "MinSimilarity": {{ .Values.Config.Walter1Assistant.MinSimilarity }},
      "CutoffContextTokens": {{ .Values.Config.Walter1Assistant.CutoffContextTokens }},
      "CutoffHistoryTokens": {{ .Values.Config.Walter1Assistant.CutoffHistoryTokens }},
      "DefaultSystemPrompt": {{ .Values.Config.Walter1Assistant.DefaultSystemPrompt | quote }},
      "DefaultContextualizePrompt": {{ .Values.Config.Walter1Assistant.DefaultContextualizePrompt | quote }},
      "Archive": "pinecone.default",
      "InputProcessors": [
        {{ .Values.Config.Walter1Assistant.TextProcessor1 | quote }},
        {{ .Values.Config.Walter1Assistant.TextProcessor2 | quote }}
      ]
    }
  },
  "DistributedLockService": {
    "PostgreSQLClientName": "pg_default",
    "LockInterval": "00:00:01",
    "RenewInterval": "00:00:10",
    "GraceInterval": "00:00:30",
    "CheckInterval": "00:00:10",
    "AcquirePollingInterval": "00:00:01"
  },
  "ProdRepos": {
    "default": {
      "PostgreSQLClientName": "pg_default"
    }
  },
  "SakenetwerkAssistant": {
    "AdminMode": true,
    "JsonStoreName": "sakenetwerk_cache",
    "OpenAIAgentName": "gpt-4o-mini",
    "CleanCitiesPrompt": "We are cleaning a list of city names in South Africa.\nRemove leading and trailing spaces, commas and other noise.\nUse the English version of the city\u0027s name whenever possible.\nConvert abbreviated city names to full city names.\nFix spelling and grammar errors.",
    "CleanProvincesPrompt": "We are cleaning a list of province names in South Africa.\nRemove leading and trailing spaces, commas and other noise.\nUse the English version of the province\u0027s name whenever possible.\nConvert abbreviated province names to full province names.\nFix spelling and grammar errors.",
    "PostgreSQLClientName": "pg_default",
    "ContextualizePrompt": "Given a user question, you need to define filters that will help us narrow down results in the Sakenetwerk database. We search with an AND filter over all of the fields that you specify. Search broadly - do not filter out anything that could eliminate valid results.\r\n\r\nFields:\r\n- NameLike: An ILIKE condition on the name of the business, or empty for no filter. Only use this filter if the user asks for a specific name.\r\n- CityLike: An array of ILIKE conditions on the name of the city in South Africa that the business is in, or empty for no filter.\r\n  -- Use the English version of the city\u0027s name whenever possible.\r\n  -- Convert abbreviated city names to full city names.\r\n- Provinces: One or more, or empty for any.\r\n- Categories: All categories that relevant businesses would list themselves under in the database.\r\n- Tags: All tags that relevant businesses would list themselves under in the database.\r\n- EmailLike: The ILIKE value on the business\u0027 email, or empty for no filter.\r\n- PhoneNumber: One full or partial phone number to search for. No conditional segments. Replace \u002B27 with 0.\r\n- UrlLike: The ILIKE value on the business\u0027 URL, or empty for no filter.\r\n- AdminCommand: An admin command to run, or None for no command.\r\n- ExplainFilter: A description of why this filter will find matches the user is interested in, and why it will not miss results that the user is interested in.",
    "ExpandPrompt": "What categories / tags do you think this business could be in?  List all candidates that a user might search for this business in and explain why.",
    "SystemPrompt": "You are Sakie, the helpful assistant for the Sakenetwerk list of enterprises in South Africa. Users may ask you to search this database on their behalf.\nFor every Listing you provide:\n- General details\n- RelevanceReasons: Determine from the business\u0027 name how likely they are to offer the specific product(s) or service(s) the user is looking for.  State the product/service and then how likely the business is to offer this kind of service: VERY LIKELY, LIKELY, POTENTIALLY, MAYBE, NOT AT ALL.\n- Relevant: Based on your reasoning above, is this business very likely to fill the request?\nLanguage: Always answer in Afrikaans.\nBoundary: Do not answer questions that are unrelated to finding relevant businesses in the database."
  },
  "ConfigJsonStoreProvider": {
    "PostgreSQLClientName": "pg_default",
    "RefreshInterval": "00:00:01",
    "RetryDelay": "00:00:01"
  },
  "WeeleeOptions": {
    "ClientId": {{ .Values.Config.Weelee.ClientId | quote }},
    "ClientSecret": {{ .Values.Config.Weelee.ClientSecret | quote }},
    "Username": {{ .Values.Config.Weelee.Username | quote }},
    "Password": {{ .Values.Config.Weelee.Password | quote }},
    "RequestUrl": {{ .Values.Config.Weelee.RequestUrl | quote }}
  },
  "StructuredDataOptions": {
    "ApiKey": {{ .Values.Config.StructuredData.ApiKey | quote }},
    "ApiSecret": {{ .Values.Config.StructuredData.ApiSecret | quote }}
  },
  "PgConsole": {
    "PostgreSQLClientName": "pg_default",
    "LuaCoreName": "lua_default"
  },
  "LuaCores": {
    "lua_default": {
      "Libraries": [
        "ServiceProvider"
      ]
    }
  },
  "ProdS3Helpers": {
    "s3_default": {
      "Region": {{ .Values.Config.BucketStorage.AwsRegion | quote }},
      "PrivateBucket": {{ .Values.Config.BucketStorage.PrivateBucketName | quote }},
      "PublicBucket": {{ .Values.Config.BucketStorage.PublicBucketName | quote }},
      "SecretAccessKey": {{ .Values.Config.BucketStorage.AwsAccessKey | quote }},
      "AccessKeyId": {{ .Values.Config.BucketStorage.AwsAccessKeyId | quote }},
      "Session": {{ .Values.Config.BucketStorage.AwsSession | quote }}
    }
  }
}