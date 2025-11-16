using FluentAssertions;
using PlaylistService.Models;
using PlaylistService.Models.Commands;
using PlaylistService.Models.Entities;

namespace PlaylistService.Test;

public class RadioMetadataTests
{
    [Fact]
    public void Create_Should_Set_All_Fields_From_Command()
    {
        var command = new CreateMetadataCommand
        {
            PrimaryId = PrimaryId.Parse("test:test:test"),
            Country = "UK",
            CityName = "London",
            CountryCode = "GB",
            TimeZone = "Europe/London",
            SubmissionFormUrl = "http://form",
            SubmissionInstructions = "Send MP3",
            MusicRequirements = "Pop only",
            SubmissionInfoUrl = "http://info",
            SubmissionFriendly = true,
            PaymentType = "PayPal",
            PaymentPrice = "10 USD",
            PaymentDetails = "Per track",
            LocalizationScope = "Global",
            LocalizationRegion = "Europe",
            LocalizationPriority = "High",
            AudienceScope = "Public",
            BroadcastFormat = "FM",
            CurationFrequency = "Weekly",
            FeedbackRate = 0.8,
            Reach = 20000
        };

        var entity = new RadioMetadata();

        entity.Create(command);

        entity.Country.Should().Be("UK");
        entity.CityName.Should().Be("London");
        entity.CountryCode.Should().Be("GB");
        entity.TimeZone.Should().Be("Europe/London");
        entity.SubmissionFormUrl.Should().Be("http://form");
        entity.SubmissionInstructions.Should().Be("Send MP3");
        entity.MusicRequirements.Should().Be("Pop only");
        entity.SubmissionInfoUrl.Should().Be("http://info");
        entity.SubmissionFriendly.Should().BeTrue();
        entity.PaymentType.Should().Be("PayPal");
        entity.PaymentPrice.Should().Be("10 USD");
        entity.PaymentDetails.Should().Be("Per track");
        entity.LocalizationScope.Should().Be("Global");
        entity.LocalizationRegion.Should().Be("Europe");
        entity.LocalizationPriority.Should().Be("High");
        entity.AudienceScope.Should().Be("Public");
        entity.BroadcastFormat.Should().Be("FM");
        entity.CurationFrequency.Should().Be("Weekly");
        entity.FeedbackRate.Should().Be(0.8);
        entity.Reach.Should().Be(20000);
        entity.Type.Should().Be("radio");
    }

    [Fact]
    public void Create_Should_Assign_Defaults_When_Values_Null()
    {
        var command = new CreateMetadataCommand()
        {
            PrimaryId = PrimaryId.Parse("test:test:test"),
        };

        var entity = new RadioMetadata();
        entity.Create(command);

        entity.Country.Should().BeEmpty();
        entity.CityName.Should().BeEmpty();
        entity.CountryCode.Should().BeEmpty();
        entity.TimeZone.Should().BeEmpty();
        entity.SubmissionFormUrl.Should().BeEmpty();
        entity.SubmissionInstructions.Should().BeEmpty();
        entity.MusicRequirements.Should().BeEmpty();
        entity.SubmissionInfoUrl.Should().BeEmpty();
        entity.SubmissionFriendly.Should().BeFalse();
        entity.PaymentType.Should().BeEmpty();
        entity.PaymentPrice.Should().BeEmpty();
        entity.PaymentDetails.Should().BeEmpty();
        entity.LocalizationScope.Should().BeEmpty();
        entity.LocalizationRegion.Should().BeEmpty();
        entity.LocalizationPriority.Should().BeEmpty();
        entity.AudienceScope.Should().BeEmpty();
        entity.BroadcastFormat.Should().BeEmpty();
        entity.CurationFrequency.Should().BeEmpty();
        entity.FeedbackRate.Should().BeNull();
        entity.Reach.Should().BeNull();
    }

    [Fact]
    public void Update_Should_Replace_Only_NonEmpty_Fields()
    {
        var initialCommand = new CreateMetadataCommand
        {
            PrimaryId = PrimaryId.Parse("test:test:test"),
            Country = "Old",
            CityName = "OldCity",
            SubmissionFriendly = false,
            Reach = 100
        };

        var entity = new RadioMetadata();
        entity.Create(initialCommand);

        var update = new UpdateMetadataCommand
        {
            Country = "NewCountry",
            CityName = "",
            SubmissionFriendly = true,
            Reach = 200
        };

        entity.Update(update);

        entity.Country.Should().Be("NewCountry");
        entity.CityName.Should().Be("OldCity");
        entity.SubmissionFriendly.Should().BeTrue();
        entity.Reach.Should().Be(200);
    }

    [Fact]
    public void Update_Should_Not_Overwrite_When_Null_Or_Whitespace()
    {
        var command = new CreateMetadataCommand
        {
            PrimaryId = PrimaryId.Parse("test:test:test"),
            Country = "USA",
            CityName = "New York",
            BroadcastFormat = "FM"
        };

        var entity = new RadioMetadata();
        entity.Create(command);

        var update = new UpdateMetadataCommand
        {
            Country = null,
            CityName = "   ",
            BroadcastFormat = ""
        };

        entity.Update(update);

        entity.Country.Should().Be("USA");
        entity.CityName.Should().Be("New York");
        entity.BroadcastFormat.Should().Be("FM");
    }

    [Fact]
    public void Update_Should_Set_All_Fields_When_Present()
    {
        var entity = new RadioMetadata();
        entity.Create(new CreateMetadataCommand()
        {
            PrimaryId = PrimaryId.Parse("test:test:test"),
            Country = "UK",
            CityName = "London",
            CountryCode = "GB",
            TimeZone = "Europe/London",
            SubmissionFormUrl = "http://form",
            SubmissionInstructions = "Send MP3",
            MusicRequirements = "Pop only",
            SubmissionInfoUrl = "http://info",
            SubmissionFriendly = true,
        });

        var update = new UpdateMetadataCommand
        {
            Country = "FR",
            CityName = "Paris",
            CountryCode = "FR",
            TimeZone = "Europe/Paris",
            SubmissionFormUrl = "url",
            SubmissionInstructions = "instr",
            MusicRequirements = "any",
            SubmissionInfoUrl = "info",
            SubmissionFriendly = false,
            PaymentType = "Card",
            PaymentPrice = "5 EUR",
            PaymentDetails = "One-time",
            LocalizationScope = "Region",
            LocalizationRegion = "FR",
            LocalizationPriority = "Medium",
            AudienceScope = "Local",
            BroadcastFormat = "Digital",
            CurationFrequency = "Daily",
            FeedbackRate = 0.9,
            Reach = 12345
        };

        entity.Update(update);

        entity.Country.Should().Be("FR");
        entity.CityName.Should().Be("Paris");
        entity.CountryCode.Should().Be("FR");
        entity.TimeZone.Should().Be("Europe/Paris");
        entity.SubmissionFormUrl.Should().Be("url");
        entity.SubmissionInstructions.Should().Be("instr");
        entity.MusicRequirements.Should().Be("any");
        entity.SubmissionInfoUrl.Should().Be("info");
        entity.SubmissionFriendly.Should().BeFalse();
        entity.PaymentType.Should().Be("Card");
        entity.PaymentPrice.Should().Be("5 EUR");
        entity.PaymentDetails.Should().Be("One-time");
        entity.LocalizationScope.Should().Be("Region");
        entity.LocalizationRegion.Should().Be("FR");
        entity.LocalizationPriority.Should().Be("Medium");
        entity.AudienceScope.Should().Be("Local");
        entity.BroadcastFormat.Should().Be("Digital");
        entity.CurationFrequency.Should().Be("Daily");
        entity.FeedbackRate.Should().Be(0.9);
        entity.Reach.Should().Be(12345);
    }
}