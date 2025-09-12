using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace CX.Engine.Common.Tests;

public class ComponentsConverterTests
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class Foo
    {
        public string X { get; set; }

        public Foo()
        {
        }

        public Foo(string x)
        {
            X = x;
        }
    }

    public class FooBar : Foo
    {
        public FooBar()
        {
        }

        public FooBar(string x) : base(x)
        {
        }
    }

    public class ObjectBased
    {
        [JsonConverter(typeof(ComponentsConverter<object>))]
        [JsonInclude]
        public Components<object> Components;
    }

    public class FooBased
    {
        [JsonConverter(typeof(ComponentsConverter<Foo>))]
        [JsonInclude]
        public Components<Foo> Components;
    }

    [Fact]
    public void GenericSerializeTests()
    {
        var tIn = new ObjectBased();
        tIn.Components = [
            1,
            "a"
        ];

        var s = JsonSerializer.Serialize(tIn);
        var tOut = JsonSerializer.Deserialize<ObjectBased>(s);
        Assert.NotNull(tOut);
        Assert.NotNull(tOut.Components);
        Assert.Equal(2, tOut.Components.Count);
        Assert.IsType<int>(tOut.Components[0]);
        Assert.IsType<string>(tOut.Components[1]);
        
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<FooBased>(s));
    }

    [Fact]
    public void ConcreteSerializeTests()
    {
        var tIn = new FooBased();
        tIn.Components = [
            new Foo("hello"),
            new FooBar("world") 
        ];
        
        var s = JsonSerializer.Serialize(tIn);
        var tOut = JsonSerializer.Deserialize<FooBased>(s);
        Assert.NotNull(tOut);
        Assert.NotNull(tOut.Components);
        Assert.Equal(2, tOut.Components.Count);
        Assert.IsType<Foo>(tOut.Components[0]);
        Assert.IsType<FooBar>(tOut.Components[1]);
        Assert.Equal("hello", ((Foo)tOut.Components[0]).X);
        Assert.Equal("world", ((FooBar)tOut.Components[1]).X);
        
        var tOutObj = JsonSerializer.Deserialize<ObjectBased>(s);
        Assert.NotNull(tOutObj);
        Assert.NotNull(tOutObj.Components);
        Assert.Equal(2, tOutObj.Components.Count);
        Assert.IsType<Foo>(tOutObj.Components[0]);
        Assert.IsType<FooBar>(tOutObj.Components[1]);
        Assert.Equal("hello", ((Foo)tOutObj.Components[0]).X);
        Assert.Equal("world", ((FooBar)tOutObj.Components[1]).X);
    }
}