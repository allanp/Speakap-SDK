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
                strpos($e->getMessage(), 'issuedAt'),
                'Expecting that the Exception messages does not contain the phrase "issuedAt".'
            );
        }
    }

    /**
     * @group raw
     */
    public function testByRawPayload()
    {
        $payLoad = 'appData=&'.
            'issuedAt=2014-03-25T10%3A27%3A03.219%2B0000&'.
            'locale=en-US&'.
            'networkEID=08e1e1eadc000e6c&userEID=08e1e1eead0dc968&'.
            'signature=qiL%2BG0Giflcudl4SjLZbLt7tKf6X2uE5vb%2Bn1ld1gwM%3D';

        $signedRequest = new SignedRequest('000a000000000006', 'legless lizards', 9999999999);

        parse_str($payLoad, $params);
        $this->assertTrue($signedRequest->validateSignature($params));
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
                http_build_query($properties, null, null, PHP_QUERY_RFC3986),
                $secret,
                true
            )
        );
    }
}