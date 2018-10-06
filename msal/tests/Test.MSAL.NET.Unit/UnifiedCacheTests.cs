﻿//----------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;
using Microsoft.Identity.Core;
using Microsoft.Identity.Core.Cache;
using Microsoft.Identity.Core.Http;
using Microsoft.Identity.Core.Instance;
using Microsoft.Identity.Core.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Test.Microsoft.Identity.Core.Unit.Mocks;
using Test.MSAL.NET.Unit.Mocks;

namespace Test.MSAL.NET.Unit
{
    [TestClass]
    public class UnifiedCacheTests
    {
        private MyReceiver _myReceiver = new MyReceiver();

        [TestInitialize]
        public void TestInitialize()
        {
            ModuleInitializer.ForceModuleInitializationTestOnly();
            Authority.ValidatedAuthorities.Clear();
            Telemetry.GetInstance().RegisterReceiver(_myReceiver.OnEvents);

            AadInstanceDiscovery.Instance.Cache.Clear();
            //AddMockResponseForInstanceDiscovery();
        }

        internal void AddMockResponseForInstanceDiscovery(MockHttpManager httpManager)
        {
            httpManager.AddMockHandler(
                MockHelpers.CreateInstanceDiscoveryMockHandler(
                    TestConstants.GetDiscoveryEndpoint(TestConstants.AuthorityCommonTenant)));
        }

        class TestLegacyCachePersistance : ILegacyCachePersistance
        {
            private byte[] data;
            public byte[] LoadCache()
            {
                return data;
            }

            public void WriteCache(byte[] serializedCache)
            {
                data = serializedCache;
            }
        }

        [TestMethod]
        [Description("Test unified token cache")]
        public void UnifiedCache_MsalStoresToAndReadRtFromAdalCache()
        {            
            MockWebUI ui = new MockWebUI()
            {
                MockResult = new AuthorizationResult(AuthorizationStatus.Success,
                    TestConstants.AuthorityHomeTenant + "?code=some-code")
            };


            using (var httpManager = new MockHttpManager())
            {
                AddMockResponseForInstanceDiscovery(httpManager);

                PublicClientApplication app = new PublicClientApplication(httpManager, TestConstants.ClientId, ClientApplicationBase.DefaultAuthority)
                {
                    UserTokenCache =
                    {
                        legacyCachePersistance = new TestLegacyCachePersistance()
                    }
                };

                MsalMockHelpers.ConfigureMockWebUI(new AuthorizationResult(AuthorizationStatus.Success,
                                                                           app.RedirectUri + "?code=some-code"));

                //add mock response for tenant endpoint discovery
                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        Method = HttpMethod.Get,
                        ResponseMessage = MockHelpers.CreateOpenIdConfigurationResponse(TestConstants.AuthorityHomeTenant)
                    });

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler
                    {
                        Method = HttpMethod.Post,
                        ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage()
                    });

                AuthenticationResult result = app.AcquireTokenAsync(TestConstants.Scope).Result;
                Assert.IsNotNull(result);

                // make sure Msal stored RT in Adal cache
                IDictionary<AdalTokenCacheKey, AdalResultWrapper> adalCacheDictionary =
                    AdalCacheOperations.Deserialize(app.UserTokenCache.legacyCachePersistance.LoadCache());

                Assert.IsTrue(adalCacheDictionary.Count == 1);

                var requestContext = new RequestContext(new MsalLogger(Guid.Empty, null));
                var users =
                    app.UserTokenCache.GetAccountsAsync(TestConstants.AuthorityCommonTenant, false, requestContext).Result;
                foreach (IAccount user in users)
                {
                    ISet<string> authorityHostAliases = new HashSet<string>();
                    authorityHostAliases.Add(TestConstants.ProductionPrefNetworkEnvironment);

                    app.UserTokenCache.RemoveMsalAccount(user, authorityHostAliases, requestContext);
                }

                httpManager.AddMockHandler(
                    new MockHttpMessageHandler()
                    {
                        Method = HttpMethod.Post,
                        PostData = new Dictionary<string, string>()
                        {
                            {"grant_type", "refresh_token"}
                        },
                        ResponseMessage = MockHelpers.CreateSuccessTokenResponseMessage(
                            TestConstants.UniqueId,
                            TestConstants.DisplayableId,
                            TestConstants.Scope.ToArray())
                    });

                // Using RT from Adal cache for silent call
                AuthenticationResult result1 = app.AcquireTokenSilentAsync(
                    TestConstants.Scope,
                    result.Account,
                    TestConstants.AuthorityCommonTenant,
                    false).Result;

                Assert.IsNotNull(result1);
            }
        }

        [TestMethod]
        [Description("Test for duplicate key in ADAL cache")]
        public void UnifiedCache_ProcessAdalDictionaryForDuplicateKeyTest()
        {
            using (var httpManager = new MockHttpManager())
            {
                AddMockResponseForInstanceDiscovery(httpManager);

                PublicClientApplication app = new PublicClientApplication(
                    httpManager,
                    TestConstants.ClientId,
                    ClientApplicationBase.DefaultAuthority)
                {
                    UserTokenCache =
                    {
                        legacyCachePersistance = new TestLegacyCachePersistance()
                    }
                };

                ISet<string> authorityHostAliases = new HashSet<string>();
                authorityHostAliases.Add(TestConstants.ProductionPrefNetworkEnvironment);

                CreateAdalCache(app.UserTokenCache.legacyCachePersistance, TestConstants.Scope.ToString());

                var tuple = CacheFallbackOperations.GetAllAdalUsersForMsal(
                    app.UserTokenCache.legacyCachePersistance,
                    authorityHostAliases,
                    TestConstants.ClientId);

                CreateAdalCache(app.UserTokenCache.legacyCachePersistance, "user.read");

                var tuple2 = CacheFallbackOperations.GetAllAdalUsersForMsal(
                    app.UserTokenCache.legacyCachePersistance,
                    authorityHostAliases,
                    TestConstants.ClientId);

                Assert.AreEqual(tuple.Item1.Keys.First(), tuple2.Item1.Keys.First());

                app.UserTokenCache.tokenCacheAccessor.AccessTokenCacheDictionary.Clear();
                app.UserTokenCache.tokenCacheAccessor.RefreshTokenCacheDictionary.Clear();
            }
        }

        private void CreateAdalCache(ILegacyCachePersistance legacyCachePersistance, string scopes)
        {
            AdalTokenCacheKey key = new AdalTokenCacheKey(TestConstants.AuthorityHomeTenant, scopes,
                TestConstants.ClientId, TestConstants.TokenSubjectTypeUser, TestConstants.UniqueId, TestConstants.User.Username);

            AdalResultWrapper wrapper = new AdalResultWrapper()
            {
                Result = new AdalResult(null, null, DateTimeOffset.MinValue)
                {
                    UserInfo = new AdalUserInfo()
                    {
                        UniqueId = TestConstants.UniqueId,
                        DisplayableId = TestConstants.User.Username
                    }
                },
                RefreshToken = TestConstants.ClientSecret,
                RawClientInfo = TestConstants.RawClientId,
                ResourceInResponse = scopes
            };

            IDictionary<AdalTokenCacheKey, AdalResultWrapper> dictionary = AdalCacheOperations.Deserialize(legacyCachePersistance.LoadCache());
            dictionary[key] = wrapper;
            legacyCachePersistance.WriteCache(AdalCacheOperations.Serialize(dictionary));
        }
    }
}
