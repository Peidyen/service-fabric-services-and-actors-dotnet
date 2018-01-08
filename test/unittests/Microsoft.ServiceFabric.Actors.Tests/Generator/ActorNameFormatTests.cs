﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.ServiceFabric.Actors.Tests.Generator
{
    using FluentAssertions;
    using Microsoft.ServiceFabric.Actors.Generator;
    using Xunit;

    public class ActorNameFormatTests
    {
        [Fact]
        public void GetFabricService_NoServiceNameProvided_ReturnServiceName()
        {
            // Arrange
            var serviceName = "ObjectActorService";

            // Act
            string result = ActorNameFormat.GetFabricServiceName(typeof(object));

            // Assert
            result.Should().Be(serviceName);
        }

        [Fact]
        public void GetFabricService_PassServiceName_ReturnServiceName()
        {
            // Arrange
            var serviceName = "serviceName";

            // Act
            string result = ActorNameFormat.GetFabricServiceName(typeof(object), serviceName);

            // Assert
            result.Should().Be(serviceName);
        }
    }
}