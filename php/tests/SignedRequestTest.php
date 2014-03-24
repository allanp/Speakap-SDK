<?php

namespace Speakap\Tests;

use Speakap\SDK\SignedRequest;

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
        $dt = new \DateTime('now');
        $dt->modify($modification);
        return $dt->format(\DATE_ISO8601);
    }

    /**
     * Return a list with default properties, optionally with certain overrides.
     *
     * @param array $customProperties
     *
     * @return array
     */
    private function getProperties(array $customProperties = array())
    {
        $properties = array(
            'appData' => null,
            'issuedAt' => (new \DateTime())->format(\DATE_ISO8601),
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
        $properties = $this->getProperties($customProperties);
        unset($properties['signature']);

        $properties['signature'] = hash_hmac('sha256', rawurlencode(http_build_query($properties)), $secret, false);

        return rawurlencode(http_build_query($properties));
    }
}