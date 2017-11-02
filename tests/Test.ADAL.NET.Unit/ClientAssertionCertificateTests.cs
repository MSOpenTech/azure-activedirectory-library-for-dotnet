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

using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Helpers;
using Microsoft.IdentityModel.Clients.ActiveDirectory.Internal.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Test.ADAL.Common;
using Test.ADAL.NET.Common;
using Test.ADAL.NET.Common.Mocks;

namespace Test.ADAL.NET.Unit
{
    [TestClass]
    [DeploymentItem("valid_cert.pfx")]
    public class ClientAssertionCertificateTests
    {
        private PlatformParameters platformParameters;

        [TestInitialize]
        public void Initialize()
        {
            HttpMessageHandlerFactory.ClearMockHandlers();
            InstanceDiscovery.InstanceCache.Clear();
            HttpMessageHandlerFactory.AddMockHandler(MockHelpers.CreateInstanceDiscoveryMockHandler(TestConstants.GetDiscoveryEndpoint(TestConstants.DefaultAuthorityCommonTenant)));
            platformParameters = new PlatformParameters(PromptBehavior.Auto);
        }

        [TestMethod]
        [Description("Test for Client assertion with X509")]
        public async Task ClientAssertionWithX509Test()
        {
            var certificate = new X509Certificate2("valid_cert.pfx", TestConstants.DefaultPassword);
            var clientAssertion = new ClientAssertionCertificate(TestConstants.DefaultClientId, certificate);

            var context = new AuthenticationContext(TestConstants.DefaultAuthorityCommonTenant, new TokenCache());
            var expectedAudience = TestConstants.DefaultAuthorityCommonTenant + "oauth2/token";

            var ValidX5cClaim = "\"x5c\":\"W1N1YmplY3RdDQogIENOPUFDUzJDbGllbnRDZXJ0aWZpY2F0ZQ0KDQpbSXNzdWVyXQ0KICBDTj1BQ1MyQ2xpZW50Q2VydGlmaWNhdGUNCg0KW1NlcmlhbCBOdW1iZXJdDQogIDMzQTM0NTYwRDA0OUY2Qjc0RTg4QUY4MkY3NTY3MzE0DQoNCltOb3QgQmVmb3JlXQ0KICA1LzIyLzIwMTIgMzoxMToyMiBQTQ0KDQpbTm90IEFmdGVyXQ0KICA1LzIyLzIwMzAgMTI6MDA6MDAgQU0NCg0KW1RodW1icHJpbnRdDQogIDQyOUFCMDI1RkFDQzgwQ0VGMzA3MURENzc3NzcxMTFENjc1QTQyNTMNCg";

            HttpMessageHandlerFactory.AddMockHandler(new MockHttpMessageHandler(TestConstants.GetTokenEndpoint(TestConstants.DefaultAuthorityCommonTenant))
            {
                Method = HttpMethod.Post,
                ResponseMessage = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"token_type\":\"Bearer\",\"expires_in\":\"3599\",\"access_token\":\"some-access-token\"}")
                },
                PostData = new Dictionary<string, string>
                {
                    {"client_id", TestConstants.DefaultClientId},
                    {"grant_type", "client_credentials"},
                    {"client_assertion_type", "urn:ietf:params:oauth:client-assertion-type:jwt-bearer"}
                },
                AdditionalRequestValidation = request =>
                {
                    var requestContent = request.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    var formsData = EncodingHelper.ParseKeyValueList(requestContent, '&', true, null);

                    // Check presence of client_assertion in request
                    string encodedJwt;
                    Assert.IsTrue(formsData.TryGetValue("client_assertion", out encodedJwt), "Missing client_assertion from request");

                    // Check presence of x5c cert claim
                    var jwt = EncodingHelper.Base64Decode(encodedJwt.Split('.')[0]);
                    Assert.IsTrue(jwt.Contains(ValidX5cClaim));
                }
            });

            AuthenticationResult result = await context.AcquireTokenAsync(TestConstants.DefaultResource, clientAssertion);
            Assert.IsNotNull(result.AccessToken);

            // Null resource -> error
            var exc = AssertException.TaskThrows<ArgumentNullException>(() =>
                context.AcquireTokenAsync(null, clientAssertion));
            Assert.AreEqual(exc.ParamName, "resource");

            // Null client credential -> error
            exc = AssertException.TaskThrows<ArgumentNullException>(() =>
                context.AcquireTokenAsync(TestConstants.DefaultResource, (ClientCredential)null));

            Assert.AreEqual(exc.ParamName, "clientCredential");
        }
    }
}
