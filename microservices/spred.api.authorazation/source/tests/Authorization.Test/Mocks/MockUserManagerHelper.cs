using Microsoft.AspNetCore.Identity;
using Moq;
using System.Collections.Generic;

namespace Authorization.Test.Mocks;

public static class MockUserManagerHelper
{
    public static Mock<UserManager<TUser>> CreateMock<TUser>() where TUser : class
    {
        var store = new Mock<IUserStore<TUser>>();
        var userValidators = new List<IUserValidator<TUser>>();
        var pwdValidators = new List<IPasswordValidator<TUser>>();

        return new Mock<UserManager<TUser>>(
            store.Object,
            null!, // IOptions<IdentityOptions>
            null!, // IPasswordHasher<TUser>
            userValidators,
            pwdValidators,
            null!, // ILookupNormalizer
            null!, // IdentityErrorDescriber
            null!, // IServiceProvider
            null!  // ILogger<UserManager<TUser>>
        );
    }
}
