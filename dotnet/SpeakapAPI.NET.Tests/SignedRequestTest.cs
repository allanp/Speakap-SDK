using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Speakap.SDK;

namespace SpeakapAPI.NET.Tests
{
    [TestClass]
    public class SignedRequestTest
    {
        internal const string ISO8601DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fff+0000";
        internal const string ExpectedExceptioinButNonWasThrown = "Expected an exception, but none was thrown.";

        [TestMethod]
        public void InvalidWindowButValidSecretTest()
        {
            foreach (DateTime issuedAt in InvalidWindowProvider())
            {
                try
                {
                    var parameters = GetSignedParameters("secret", new Dictionary<string, string> { { "issuedAt", issuedAt.ToString(ISO8601DateTimeFormat) } });

                    var signedRequest = new SignedRequest("secret", 60);

                    signedRequest.ValidateSignature(parameters);

                    Assert.Fail(ExpectedExceptioinButNonWasThrown);
                }
                catch (SpeakapSignatureValidationException)
                {
                }
            }
        }

        /// <summary>
        /// Returns a list with various invalid windows
        /// </summary>
        /// <returns>array</returns>
        private static IEnumerable<DateTime> InvalidWindowProvider()
        {
            var now = DateTime.Now;
            return new[]{
                            now.AddSeconds(-61),
                            now.AddDays(-1),
                            now.AddYears(-1),
                            now.AddYears(-100),
                            // Anything positive should fail
                            now.AddSeconds(10),
                            now.AddSeconds(61),
                            now.AddDays(1),
                            now.AddYears(1),
                            now.AddYears(100)
                        };
        }

        [TestMethod]
        public void InvalidSecretButValidWindowTest()
        {
            try
            {
                var parameters = GetSignedParameters("invalid secret");

                var signedRequest = new SignedRequest("secret", 60);

                signedRequest.ValidateSignature(parameters);

                Assert.Fail(ExpectedExceptioinButNonWasThrown);
            }
            catch (SpeakapSignatureValidationException)
            {
            }
        }

        [TestMethod]
        public void InvalidSecretAndInvalidWindowTest()
        {
            try
            {
                var parameters = GetSignedParameters("invalid secret", new Dictionary<string, string> { { "issuedAt", DateTime.Now.ToString(ISO8601DateTimeFormat) } });

                var signedRequest = new SignedRequest("secret", 60);

                signedRequest.ValidateSignature(parameters);

                Assert.Fail(ExpectedExceptioinButNonWasThrown);
            }
            catch (SpeakapSignatureValidationException)
            {
            }
        }

        [TestMethod]
        public void ValidInputTest()
        {
            foreach (DateTime issuedAt in ValidWindowProvider())
            {
                try
                {
                    var parameters = GetSignedParameters("secret", new Dictionary<string, string> { { "issuedAt", issuedAt.ToString(ISO8601DateTimeFormat) } });

                    var signedRequest = new SignedRequest("secret", 60);

                    signedRequest.ValidateSignature(parameters);

                    Assert.Fail(ExpectedExceptioinButNonWasThrown);
                }
                catch (SpeakapSignatureValidationException)
                {
                }
            }
        }

        /// <summary>
        /// Returns a list with various valid windows
        /// </summary>
        /// <returns></returns>
        private static IEnumerable<DateTime> ValidWindowProvider()
        {
            return new[]
                       {
                          DateTime.Now.AddSeconds(-58),                 
                          DateTime.Now.AddSeconds(-55),
                          DateTime.Now.AddSeconds(-30),
                          DateTime.Now.AddSeconds(-1)
                       };
        }

        [TestMethod]
        public void IfInvalidArgumentsThrowAnExceptionTest()
        {
            var parameters = GetDefaultParameters();
            parameters.Remove("issuedAt");

            var signedRequest = new SignedRequest("secret", 60);

            try
            {
                signedRequest.ValidateSignature(parameters);
            }
            catch (SpeakapSignatureValidationException ex)
            {
                Assert.IsFalse(ex.Message.Contains("issuedAt="),
                    "Expecting that the Exception messages does not contain the phrase \"issuedAt=\"");

                Assert.IsTrue(ex.Message.Contains("signature="),
                    "Expecting that the Exception messages does contain the phrase \"signature=\"");
            }
        }

        [TestMethod]
        public void ByRawPayloadTest()
        {
            var signedRequest = new SignedRequest("legless lizards", 9999999999);

            foreach (var rawParameters in RawParametersProvider())
            {
                var parameters = OurParseStr(rawParameters);
                try
                {
                    signedRequest.ValidateSignature(parameters);
                }
                catch (SpeakapSignatureValidationException)
                {
                    Assert.Fail();
                }
            }
        }

        private static IEnumerable<string> RawParametersProvider()
        {
            return new[]
            {
                "appData=&issuedAt=2014-04-02T13%3A20%3A09.066%2B0000&locale=en-US&role=user&networkEID=08e1e1eadc000e6c&userEID=08e1e1eead0dc968&signature=lqWV60eaUcwhrVX%2FK7llLBzoTwYFh%2Fg78CR0TUHTPmA%3D",
                "appData=&issuedAt=2014-04-02T13%3A22%3A09.100%2B0000&locale=en-US&role=user&networkEID=08e1e1eadc000e6c&userEID=08e1e1eead0dc968&signature=36fezwghQU7tub4FuSGXeT7ggczX95o1oZCp%2BRLR9Fk%3D",
                "appData=&issuedAt=2014-04-02T13%3A22%3A35.352%2B0000&locale=en-US&role=user&networkEID=08e1e1eadc000e6c&userEID=08e1e1eead0dc968&signature=zVGVDMrO7Erm1KjBroVehRKeoeNCnIH6sEc5quX9kOo%3D",
                "appData=&issuedAt=2014-04-02T13%3A22%3A52.278%2B0000&locale=en-US&role=user&networkEID=08e1e1eadc000e6c&userEID=08e1e1eead0dc968&signature=LbP%2BXr%2Bi6kUi7wHtjidTs1nEUnIAS%2FdOJVBjLvyiZko%3D",
                "appData=&issuedAt=2014-04-02T13%3A23%3A16.805%2B0000&locale=en-US&role=user&networkEID=08e1e1eadc000e6c&userEID=08e1e1eead0dc968&signature=vLoL0yr0CkI81Qso7jQOi%2FTmgXTWqe7JVo98dH%2FG59M%3D",
                "appData=&issuedAt=2014-04-02T13%3A23%3A54.824%2B0000&locale=en-US&role=user&networkEID=08e1e1eadc000e6c&userEID=08e1e1eead0dc968&signature=fifTbn8s%2Bhsg0LIiyDbrbf28Xkredi1gFQoqt2LkioM%3D"
            };
        }

        [TestMethod]
        public void GetSignedParametersTest()
        {
            const string secret = "secret";
            var signedRequest = new SignedRequest(secret, 9999999999);
            var parameters = GetDefaultParameters();

            var verifyParameters = GetSignedParameters(secret, parameters);
            var signedParameters = signedRequest.GetSignedParameters(parameters);

            Assert.AreEqual(verifyParameters["signature"], signedParameters["signature"]);
        }

        /// <summary>
        /// Return a list with default properties, optionally with certain overrides.
        /// </summary>
        /// <param name="customParameters"></param>
        /// <returns></returns>
        private static IDictionary<string, string> GetDefaultParameters(IDictionary<string, string> customParameters = null)
        {
            var parameters = new Dictionary<string, string>
                                 {
                                     {"appData", ""},
                                     {"issuedAt", DateTime.Now.ToString(ISO8601DateTimeFormat)},
                                     {"locale", "en-US"},
                                     {"role", "user"},
                                     {"networkEID", "0000000000000001"},
                                     {"userEID", "0000000000000002"},
                                     {"signature", null},
                                 };

            if (customParameters != null)
            {
                foreach (var parameter in customParameters)
                {
                    if (parameter.Key == null)
                        continue;

                    if (parameters.ContainsKey(parameter.Key))
                    {
                        parameters[parameter.Key] = parameter.Value;
                    }
                    else
                    {
                        parameters.Add(parameter.Key, parameter.Value);
                    }
                }
            }

            return parameters;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="secret"></param>
        /// <param name="customParameters"></param>
        /// <returns></returns>
        private static IDictionary<string, string> GetSignedParameters(string secret, IDictionary<string, string> customParameters = null)
        {
            var parameters = GetDefaultParameters(customParameters);

            parameters["signature"] = GetSignature(secret, parameters);

            return parameters;
        }

        /// <summary>
        /// Generate the signature from an array of properties
        /// </summary>
        /// <param name="secret"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private static string GetSignature(string secret, IDictionary<string, string> parameters)
        {
            if (string.IsNullOrEmpty(secret))
                throw new ArgumentException("secret");

            if (parameters == null)
                throw new ArgumentNullException("parameters");

            string signature;
            var hasSignature = parameters.TryGetValue("signature", out signature);

            if (hasSignature)
                parameters.Remove("signature");

            var query = string.Join("&", parameters.OrderBy(p => p.Key).Select(p => string.Format("{0}={1}", p.Key, p.Value == null ? "" : Uri.EscapeDataString(p.Value))));

            if (string.IsNullOrEmpty(query))
                throw new ArgumentException("query");

            var inArray = new System.Security.Cryptography.HMACSHA256(Encoding.UTF8.GetBytes(secret)).ComputeHash(Encoding.UTF8.GetBytes(query));

            if (inArray == null)
                throw new ArgumentException("inArray");

            var sign = Convert.ToBase64String(inArray);

            if (hasSignature)
                parameters.Add("signature", signature);

            return sign;
        }

        /// <summary>
        /// Follows the signature of parse_str() with the exception that it doesn't use urldecode() but rawurldecode()
        /// </summary>
        /// <param name="rawParameters"></param>
        /// <returns></returns>
        private static IDictionary<string, string> OurParseStr(string rawParameters)
        {
            if (string.IsNullOrEmpty(rawParameters))
                throw new ArgumentNullException("rawParameters");

            var pairs = rawParameters.Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries);

            var parameters = new Dictionary<string, string>();

            foreach (var pair in pairs)
            {
                if (string.IsNullOrEmpty(pair))
                    continue;

                var index = pair.IndexOf('=');
                if (index <= 0)
                    continue;

                var key = pair.Substring(0, index);
                if (key == null)
                    continue;

                var value = pair.Length <= index ? "" : Uri.UnescapeDataString(pair.Substring(index + 1));

                if (!parameters.ContainsKey(key))
                    parameters.Add(key, value);
                else
                    parameters[key] = value;
            }

            return parameters;
        }
    }
}
