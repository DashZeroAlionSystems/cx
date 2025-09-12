using System.Text.Json;
using CX.Engine.Common.Json;

namespace CX.Engine.Common.Tracing.Langfuse;

public class LangfuseResponse
{
    public readonly Dictionary<string, (int code, string message, string error)> Errors = new();

    public LangfuseResponse(byte[] bytes)
    {
        var jr = new Utf8JsonReader(bytes);
        PopulateFromReader(ref jr);
    }

    public void PopulateFromReader(ref Utf8JsonReader jr)
    {
        void ReadSuccessStatuses(ref Utf8JsonReader jr)
        {
            jr.ReadArrayOfObject(true,
                (ref Utf8JsonReader jr) =>
                {
                    var id = "";
                    var status = -1;
                    // ReSharper disable once VariableHidesOuterVariable
                    jr.ReadObjectProperties(null,
                        false,
                        (ref Utf8JsonReader jr, object _, string name) =>
                        {
                            switch (name)
                            {
                                case "id":
                                    id = jr.ReadStringValue()!;
                                    break;
                                case "status":
                                    status = jr.ReadInt32Value();
                                    break;
                                default:
                                    jr.SkipPropertyValue();
                                    break;
                            }
                        });
                    if (status < 0)
                        throw new JsonException("status is required");
                    if (string.IsNullOrWhiteSpace(id))
                        throw new JsonException("id is required");
                });
        }
        void ReadErrorStatuses(ref Utf8JsonReader jr)
        {
            jr.ReadArrayOfObject(true,
                (ref Utf8JsonReader jr) =>
                {
                    var id = "";
                    var status = -1;
                    var message = "";
                    var error = "";
                    // ReSharper disable once VariableHidesOuterVariable
                    jr.ReadObjectProperties(null,
                        false,
                        (ref Utf8JsonReader jr, object _, string name) =>
                        {
                            switch (name)
                            {
                                case "id":
                                    id = jr.ReadStringValue()!;
                                    break;
                                case "status":
                                    status = jr.ReadInt32Value();
                                    break;
                                case "message":
                                    message = jr.ReadStringValue();
                                    break;
                                case "error":
                                    error = jr.ReadStringValue();
                                    break;
                                default:
                                    jr.SkipPropertyValue();
                                    break;
                            }
                        });
                    if (status < 0)
                        throw new JsonException("status is required");
                    if (string.IsNullOrWhiteSpace(id))
                        throw new JsonException("id is required");
                    Errors[id] = (status, message, error);
                });
        }

        jr.ReadObjectProperties(this,
            true,
            (ref Utf8JsonReader jr, LangfuseResponse _, string name) =>
            {
                switch (name)
                {
                    case "errors":
                        ReadErrorStatuses(ref jr);
                        break;
                    case "successes":
                        ReadSuccessStatuses(ref jr);
                        break;
                    default:
                        jr.SkipPropertyValue();
                        break;
                }
            });
    }
}