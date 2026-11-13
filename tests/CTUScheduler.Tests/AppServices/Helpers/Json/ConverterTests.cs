using System;
using System.Collections.Generic;
using System.Text.Json;
using CTUScheduler.AppServices.Helpers.Json;
using FluentAssertions;
using Xunit;

namespace CTUScheduler.Tests.AppServices.Helpers.Json;

public class ConverterTests
{
    [Fact]
    public void SafeGuidConverter_WhenValidGuid_ShouldDeserializeCorrectly()
    {
        var expectedGuid = Guid.NewGuid();
        var json = $"\"{expectedGuid}\"";
        var options = new JsonSerializerOptions();
        options.Converters.Add(new SafeGuidConverter());

        var result = JsonSerializer.Deserialize<Guid>(json, options);

        result.Should().Be(expectedGuid);
    }

    [Fact]
    public void SafeGuidConverter_WhenInvalidGuid_ShouldFallbackToNewGuidWithoutThrowing()
    {
        var json = "\"not-a-valid-guid\"";
        var options = new JsonSerializerOptions();
        options.Converters.Add(new SafeGuidConverter());

        var result = JsonSerializer.Deserialize<Guid>(json, options);

        result.Should().NotBeEmpty();
    }

    [Fact]
    public void StrictDictionaryConverter_WhenValid_ShouldDeserializeDictionary()
    {
        var json = "{\"key1\":\"val1\",\"key2\":\"val2\"}";
        var options = new JsonSerializerOptions();
        options.Converters.Add(new StrictDictionaryConverter());

        var result = JsonSerializer.Deserialize<Dictionary<string, string>>(json, options);

        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result!["key1"].Should().Be("val1");
        result["key2"].Should().Be("val2");
    }

    [Fact]
    public void StrictDictionaryConverter_WhenDuplicateKeys_ShouldThrowInvalidOperationException()
    {
        var json = "{\"key1\":\"val1\",\"key1\":\"val2\"}";
        var options = new JsonSerializerOptions();
        options.Converters.Add(new StrictDictionaryConverter());

        Action act = () => JsonSerializer.Deserialize<Dictionary<string, string>>(json, options);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Duplicate key found*");
    }
}
