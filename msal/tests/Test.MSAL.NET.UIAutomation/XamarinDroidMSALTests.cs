﻿using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Test.Microsoft.Identity.Core.UIAutomation;
using Test.Microsoft.Identity.Core.UIAutomation.infrastructure;
using Xamarin.UITest;

namespace Test.MSAL.NET.UIAutomation
{
    /// <summary>
    /// Configures environment for core/android tests to run
    /// </summary>
    [TestFixture(Platform.Android)]
    class XamarinMSALDroidTests
    {
        IApp app;
        Platform platform;
        ITestController xamarinController;

        public XamarinMSALDroidTests(Platform platform)
        {
            this.platform = platform;
        }

        /// <summary>
        /// Initializes test controller
        /// </summary>
        [SetUp]
        public void BeforeEachTest()
        {
            app = AppFactory.StartApp(platform);
            if (xamarinController == null)
                xamarinController = new XamarinUITestController(app);
        }

        /// <summary>
        /// Runs through the standard acquire token flow
        /// </summary>
        [Test]
        public void AcquireTokenTest()
        {
            CoreMobileMSALTests.AcquireTokenTest(xamarinController);
        }
    }
}
