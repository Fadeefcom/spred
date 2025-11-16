using System.Linq;
using Authorization.Helpers;
using Authorization.Options.AuthenticationSchemes;
using Spred.Bus.Contracts;

namespace Authorization.Test;

public class AccountPlatformHelperTests
    {
        [Theory]
        [InlineData("spotify", AccountPlatform.Spotify)]
        [InlineData("Spotify", AccountPlatform.Spotify)]
        [InlineData("SPOTIFY", AccountPlatform.Spotify)]
        [InlineData("apple-music", AccountPlatform.AppleMusic)]
        [InlineData("Deezer", AccountPlatform.Deezer)]
        [InlineData("soundcloud", AccountPlatform.SoundCloud)]
        [InlineData("youtube-music", AccountPlatform.YouTubeMusic)]
        public void PlatformMap_Should_Map_KnownSlugs_CaseInsensitive(string key, AccountPlatform expected)
        {
            var ok = AccountPlatformHelper.PlatformMap.TryGetValue(key, out var actual);
            Assert.True(ok);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void ReverseMap_Should_Be_Bijection_Of_PlatformMap()
        {
            var forward = AccountPlatformHelper.PlatformMap;
            var reverse = AccountPlatformHelper.ReverseMap;

            Assert.Equal(forward.Count, reverse.Count);

            foreach (var (slug, platform) in forward)
            {
                Assert.True(reverse.TryGetValue(platform, out var back));
                Assert.Equal(slug, back);
            }

            var distinctPlatforms = forward.Values.Distinct().Count();
            Assert.Equal(forward.Count, distinctPlatforms);
        }

        [Fact]
        public void PlatformSchemeMap_Should_Map_RegisteredSchemes()
        {
            Assert.True(AccountPlatformHelper.PlatformSchemeMap.TryGetValue(YoutubeAuthenticationDefaults.AuthenticationScheme, out var yt));
            Assert.Equal(AccountPlatform.YouTubeMusic, yt);

            Assert.True(AccountPlatformHelper.PlatformSchemeMap.TryGetValue(SoundCloudAuthenticationDefaults.AuthenticationScheme, out var sc));
            Assert.Equal(AccountPlatform.SoundCloud, sc);
        }

        [Fact]
        public void PlatformMap_Should_Not_Contain_Empty_Or_Whitespace_Keys()
        {
            Assert.DoesNotContain(AccountPlatformHelper.PlatformMap.Keys, k => string.IsNullOrWhiteSpace(k));
        }

        [Fact]
        public void ReverseMap_Should_Expose_Canonical_Lowercase_Slugs()
        {
            foreach (var (platform, slug) in AccountPlatformHelper.ReverseMap)
            {
                Assert.Equal(slug, slug.ToLowerInvariant());
                Assert.True(AccountPlatformHelper.PlatformMap.ContainsKey(slug));
                Assert.Equal(platform, AccountPlatformHelper.PlatformMap[slug]);
            }
        }

        [Fact]
        public void Public_Static_Readonly_Dictionaries_Should_Not_Be_Null()
        {
            Assert.NotNull(AccountPlatformHelper.PlatformMap);
            Assert.NotNull(AccountPlatformHelper.ReverseMap);
            Assert.NotNull(AccountPlatformHelper.PlatformSchemeMap);
        }
    }