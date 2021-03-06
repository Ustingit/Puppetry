﻿
namespace Puppetry.PuppetContracts
{
    public static class ErrorCodes
    {
        public const int Success = 0;
        public const int SessionCreationFailed = 4;
        public const int MethodNotSupported = 5;
        public const int UnexpectedResponse = 7;
        public const int NoSuchGameObjectFound = 9;
        public const int MethodNodeIsNotPresent = 11;
        public const int PlayModeIsNotStarted = 12;
        public const int PuppetDriverError = 20;
    }
}
