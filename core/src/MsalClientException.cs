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

using System;
using System.Collections.Generic;
using System.Globalization;

namespace Microsoft.Identity.Client
{
    /// <summary>
    /// This exception class represents errors that are local to the library or the device.
    /// </summary>
    public class MsalClientException : MsalException
    {
        /// <summary>
        /// MultipleTokensMatched were matched.
        /// </summary>
        public const string MultipleTokensMatchedError = "multiple_matching_tokens_detected";

        /// <summary>
        /// Non HTTPS redirects are not supported.
        /// </summary>
        public const string NonHttpsRedirectNotSupported = "non_https_redirect_failed";

        /// <summary>
        /// The request could not be preformed because the network is down.
        /// </summary>
        public const string NetworkNotAvailableError = "network_not_available";

        /// <summary>
        /// Duplicate query parameter in extraQueryParameters
        /// </summary>
        public const string DuplicateQueryParameterError = "duplicate_query_parameter";


        /// <summary>
        /// The request could not be preformed because of a failure in the UI flow.
        /// </summary>
        public const string AuthenticationUiFailedError = "authentication_ui_failed";

        /// <summary>
        /// Authentication canceled.
        /// </summary>
        public const string AuthenticationCanceledError = "authentication_canceled";
        
        /// <summary>
        /// JSON parsing failed.
        /// </summary>
        public const string JsonParseError = "json_parse_failed";

        /// <summary>
        /// JWT was invalid
        /// </summary>
        public const string InvalidJwtError = "invalid_jwt";

        /// <summary>
        /// State returned from the STS was different than the one sent.
        /// </summary>
        public const string StateMismatchError = "state_mismatch";

        /// <summary>
        /// Tenant discovery failed.
        /// </summary>
        public const string TenantDiscoveryFailedError = "tenant_discovery_failed";

#if ANDROID

        /// <summary>
        /// Indicates that chrome is not installed on the device. The sdk uses chrome custom tab for
        /// authorize request if applicable or fall back to chrome browser.
        /// </summary>
        public const string ChromeNotInstalledError = "chrome_not_installed";

        /// <summary>
        /// The intent to launch AuthenticationActivity is not resolvable by the OS or the intent.
        /// </summary>
        public const string UnresolvableIntentError = "unresolvable_intent";

#endif


        /// <summary>
        /// Initializes a new instance of the exception class with a specified
        /// error code.
        /// </summary>
        /// <param name="errorCode">
        /// The error code returned by the service or generated by client. This is the code you can rely on
        /// for exception handling.</param>
        public MsalClientException(string errorCode) : base(errorCode)
        {
        }

        /// <summary>
        /// Initializes a new instance of the exception class with a specified
        /// error code and error message.
        /// </summary>        
        /// <param name="errorCode">
        /// The error code returned by the service or generated by client. This is the code you can rely on
        /// for exception handling.
        /// </param>
        /// <param name="errorMessage">The error message that explains the reason for the exception.</param>
        public MsalClientException(string errorCode, string errorMessage):base(errorCode, errorMessage)
        {
        }

        /// <summary>
        /// Initializes a new instance of the exception class with a specified
        /// error code, error message and inner exception.
        /// </summary>
        /// <param name="errorCode">
        /// The error code returned by the service or generated by client. This is the code you can rely on
        /// for exception handling.
        /// </param>
        /// <param name="errorMessage">The error message that explains the reason for the exception.</param>
        /// <param name="innerException"></param>
        public MsalClientException(string errorCode, string errorMessage, Exception innerException):base(errorCode, errorMessage, innerException)
        {
        }
    }
}
