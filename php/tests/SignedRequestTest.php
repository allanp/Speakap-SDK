<?php

namespace Speakap\Tests;

use Speakap\SDK\SignedRequest;
use Speakap\Date\ExtendedDateTime;

/**
 * Speakap 2014
 */
class SignedRequestTest extends \PHPUnit_Framework_TestCase
{
    /**
     * @dataProvider invalidWindowProvider
     *
     * @param string $issuedAt A ISO8601 formatted string
     *
     * @expectedException Speakap\SDK\Exception\ExpiredSignatureException
     */
    public function testInvalidWindowButValidSecret($issuedAt)
    {
        $params = $this->getSignedPayloadAsArray('secret', array('issuedAt' => $issuedAt));

        $signedRequest = new SignedRequest('foo', 'secret', 60);
        $signedRequest->validateSignature($params);

        $this->fail('Expected an exception, but non was thrown.');
    }

    /**
     * Returns a list with various invalid windows
     *
     * @return array
     */
    public function invalidWindowProvider()
    {
        return array(
            array($this->createISO8601FromModification('-61 seconds')),
            array($this->createISO8601FromModification('-1 day')),
            array($this->createISO8601FromModification('-1 year')),
            array($this->createISO8601FromModification('-100 year')),

            // Anything positive should fail
            array($this->createISO8601FromModification('+10 second')),
            array($this->createISO8601FromModification('+61 seconds')),
            array($this->createISO8601FromModification('+1 day')),
            array($this->createISO8601FromModification('+1 year')),
            array($this->createISO8601FromModification('+100 years')),
        );
    }

    /**
     * @expectedException Speakap\SDK\Exception\InvalidSignatureException
     */
    public function testInvalidSecretButValidWindow()
    {
        $params = $this->getSignedPayloadAsArray('invalid secret');

        $signedRequest = new SignedRequest('foo', 'secret', 60);
        $signedRequest->validateSignature($params);

        $this->fail('Expected an exception, but non was thrown.');
    }

    /**
     * Expecting either InvalidSignatureException or ExpiredSignatureException, both extend InvalidArgumentException.
     *
     * @expectedException \InvalidArgumentException
     */
    public function testInvalidSecretAndInvalidWindow()
    {
        $params = $this->getSignedPayloadAsArray(
            'invalid secret',
            array(
                 'issuedAt' => $this->createISO8601FromModification('-61 seconds')
            )
        );

        $signedRequest = new SignedRequest('foo', 'secret', 60);
        $signedRequest->validateSignature($params);

        $this->fail('Expected an exception, but non was thrown.');
    }

    /**
     * @dataProvider validWindowProvider
     */
    public function testValidInput($issuedAt)
    {
        $params = $this->getSignedPayloadAsArray('secret', array('issuedAt' => $issuedAt));

        $signedRequest = new SignedRequest('foo', 'secret', 60);

        $this->assertTrue( $signedRequest->validateSignature($params) );
    }

    /**
     * Returns a list with various valid windows
     *
     * @return array
     */
    public function validWindowProvider()
    {
        return array(
            array($this->createISO8601FromModification('-58 seconds')),
            array($this->createISO8601FromModification('-55 seconds')),
            array($this->createISO8601FromModification('-30 seconds')),
            array($this->createISO8601FromModification('-1 second')),
        );
    }

    public function testIfInvalidArgumentsThrowAnException()
    {
        $properties = $this->getDefaultProperties();

        // Remove an expected property
        unset($properties['issuedAt']);

        $signedRequest = new SignedRequest('foo', 'secret', 60);
        try {
            $signedRequest->validateSignature($properties);
        } catch (\InvalidArgumentException $e) {
            $this->assertFalse(
                strpos($e->getMessage(), '[issuedAt] =>'),
                'Expecting that the Exception messages does not contain the phrase "[issuedAt] =>".'
            );

            $this->assertTrue(
                false !== strpos($e->getMessage(), '[signature] =>'),
                'Expecting that the Exception messages does contain the phrase "[signature] =>".'
            );
        }
    }

    /**
     * @dataProvider rawPayLoadProvider
     */
    public function testByRawPayload($payLoad)
    {
        $signedRequest = new SignedRequest('000a000000000006', 'legless lizards', 9999999999);

        $this->ourParseStr($payLoad, $params);
        $this->assertTrue($signedRequest->validateSignature($params));
    }

    public function rawPayLoadProvider()
    {
        return array(
            array('appData=&issuedAt=2014-04-02T13%3A20%3A09.066%2B0000&locale=en-US&networkEID=08e1e1eadc000e6c&userEID=08e1e1eead0dc968&signature=bx3MowIvCQ+gVp+LvkLQ9eSVMDhiYUUtCjJaCun0GQ4%3D'),
            array('appData=&issuedAt=2014-04-02T13%3A22%3A09.100%2B0000&locale=en-US&networkEID=08e1e1eadc000e6c&userEID=08e1e1eead0dc968&signature=JqbZW8TRRPF+2crDOuDCNy1lIe08RPaAjkgLRjmupsc%3D'),
            array('appData=&issuedAt=2014-04-02T13%3A22%3A35.352%2B0000&locale=en-US&networkEID=08e1e1eadc000e6c&userEID=08e1e1eead0dc968&signature=0GZV1GV7Ujz9HRfAp7uO4vqh/NlMvaLABRN5oGpw+9c%3D'),
            array('appData=&issuedAt=2014-04-02T13%3A22%3A52.278%2B0000&locale=en-US&networkEID=08e1e1eadc000e6c&userEID=08e1e1eead0dc968&signature=ZT9VcEfqsnh0K9ogUoJovQn4qyT1wT1cz/WNzUQIEy8%3D'),
            array('appData=&issuedAt=2014-04-02T13%3A23%3A16.805%2B0000&locale=en-US&networkEID=08e1e1eadc000e6c&userEID=08e1e1eead0dc968&signature=Lwo0n+sbnyNOQ3xG899eO5jtWKsDtd8vNVxepIEbMrY%3D'),
            array('appData=&issuedAt=2014-04-02T13%3A23%3A54.824%2B0000&locale=en-US&networkEID=08e1e1eadc000e6c&userEID=08e1e1eead0dc968&signature=pwH3JBVdgBPjwWvaZXYvnqgX%2FRLj6I%2BXeIabqj%2BwkGk%3D'),
        );
    }

    public function testGetSignedParameters()
    {
        $secret = 'secret';
        $signedRequest = new SignedRequest('not relevant', $secret, 9999999999);
        $properties = $this->getDefaultProperties();

        $verifyPayload = $this->getSignedPayloadAsArray($secret, $properties);
        $signedParameters = $signedRequest->getSignedParameters($properties);

        $this->assertEquals($verifyPayload['signature'], $signedParameters['signature']);
    }

    /**
     * Modify "now" by relative terms
     *
     * @see http://www.php.net/manual/en/datetime.modify.php
     *
     * @param string $modification Example: '-1 day'
     *
     * @return string ISO8601 timestamp
     */
    private function createISO8601FromModification($modification)
    {
        $dt = new ExtendedDateTime('now');
        $dt->modify($modification);

        return $dt->format(\DateTime::ISO8601);
    }

    /**
     * Return a list with default properties, optionally with certain overrides.
     *
     * @param array $customProperties
     *
     * @return array
     */
    private function getDefaultProperties(array $customProperties = array())
    {
        $properties = array(
            'appData' => '',
            'issuedAt' => (new ExtendedDateTime())->format(\DATE_ISO8601),
            'locale' => 'en-US',
            'networkEID' => '0000000000000001',
            'userEID' => '0000000000000002',
            'signature' => null
        );

        return $customProperties + $properties;
    }

    /**
     * @param string $secret
     * @param array $customProperties
     *
     * @return array
     */
    private function getSignedPayloadAsArray($secret, array $customProperties = array())
    {
        $properties = $this->getDefaultProperties($customProperties);
        $properties['signature'] = $this->getSignature($secret, $properties);

        return $properties;
    }

    /**
     * Generate the signature from an array of properties
     *
     * @param string $secret
     * @param array $properties
     *
     * @return string
     */
    private function getSignature($secret, array $properties)
    {
        // The signature should never be part of the signature creating process.
        unset($properties['signature']);

        ksort($properties);

        return base64_encode(
            hash_hmac(
                'sha256',
                http_build_query($properties, null, '&', PHP_QUERY_RFC3986),
                $secret,
                true
            )
        );
    }

    /**
     * Follows the signature of parse_str() with the exception that it doesn't use urldecode() but rawurldecode()
     *
     * @param string $payload
     * @param array $params
     */
    private function ourParseStr($payload, &$params)
    {
        $pairs = explode("&", $payload);
        foreach ($pairs as $pair) {
            list($k, $v) = array_map("rawurldecode", explode("=", $pair));
            $params[$k] = $v;
        }
    }
}