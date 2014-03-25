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
     */
    public function testInvalidWindow($issuedAt)
    {
        $payLoad = $this->getSignedPayload('secret', array('issuedAt' => $issuedAt));

        $signedRequest = new SignedRequest('foo', 'secret', 60);
        $signedRequest->setPayload($payLoad);


        $this->assertFalse( $signedRequest->isValid() );
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
            array($this->createISO8601FromModification('+1 second')),
            array($this->createISO8601FromModification('+61 seconds')),
            array($this->createISO8601FromModification('+1 day')),
            array($this->createISO8601FromModification('+1 year')),
            array($this->createISO8601FromModification('+100 years')),
        );
    }

    public function testInvalidSecretButValidWindow()
    {
        $payLoad = $this->getSignedPayload('invalid secret');

        $signedRequest = new SignedRequest('foo', 'secret', 60);
        $signedRequest->setPayload($payLoad);

        $this->assertFalse( $signedRequest->isValid() );
    }

    public function testInvalidSecretAndInvalidWindow()
    {
        $payLoad = $this->getSignedPayload(
            'invalid secret',
            array(
                 'issuedAt' => $this->createISO8601FromModification('-61 seconds')
            )
        );

        $signedRequest = new SignedRequest('foo', 'secret', 60);
        $signedRequest->setPayload($payLoad);

        $this->assertFalse( $signedRequest->isValid() );
    }

    /**
     * @dataProvider validWindowProvider
     */
    public function testValidInput($issuedAt)
    {
        $payLoad = $this->getSignedPayload('secret', array('issuedAt' => $issuedAt));

        $signedRequest = new SignedRequest('foo', 'secret', 60);
        $signedRequest->setPayload($payLoad);

        $this->assertTrue( $signedRequest->isValid() );
    }

    /**
     * Returns a list with various valid windows
     *
     * @return array
     */
    public function validWindowProvider()
    {
        return array(
            array($this->createISO8601FromModification('-60 seconds')),
            array($this->createISO8601FromModification('-59 seconds')),
            array($this->createISO8601FromModification('-30 seconds')),
            array($this->createISO8601FromModification('-1 second')),
        );
    }

    public function testIfToStringWorksAsExpected()
    {
        $payLoad = $this->getSignedPayload('secret');

        $signedRequest = new SignedRequest('foo', 'secret', 60);
        $signedRequest->setPayload($payLoad);

        $this->assertEquals($payLoad, (string) $signedRequest);
    }

    /**
     * @expectedException \InvalidArgumentException
     */
    public function testIfInvalidArgumentsThrowAnException()
    {
        $properties = $this->getDefaultProperties();

        // Remove an expected property
        unset($properties['issuedAt']);

        $properties['signature'] = $this->getSignature('secret', $properties);
        $payLoad = rawurlencode(http_build_query($properties));


        $signedRequest = new SignedRequest('foo', 'secret', 60);
        $signedRequest->setPayload($payLoad);
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
            'signature=ZjAwZGZhZWFmYTRhNDcwOWQ0NWU4NjJjZjA3NDcxOTBlNDIyOWRjN2VkYjE3ZDhlZTU5Yzg5ZGI4ZDBkM2RkMw%3D%3D';

        $signedRequest = new SignedRequest('000a000000000006', 'legless lizards', 9999999999);
        $signedRequest->setPayload($payLoad);

        $this->assertTrue($signedRequest->isValid());
    }

    /**
     * Modify "now" by relative terms
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
     * Provide a url-encoded and signed payload, based on the supplied and default properties
     *
     * @param $secret
     * @param array $customProperties
     *
     * @return string
     */
    private function getSignedPayload($secret, array $customProperties = array())
    {
        $properties = $this->getDefaultProperties($customProperties);
        $properties['signature'] = $this->getSignature($secret, $properties);

        return http_build_query($properties);
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

        return base64_encode(hash_hmac('sha256', http_build_query($properties), $secret, false));
    }
}