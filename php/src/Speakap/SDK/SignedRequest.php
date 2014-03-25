<?php

namespace Speakap\SDK;

use \Speakap\Date\ExtendedDateTime;
use Speakap\SDK\Exception\ExpiredSignatureException;
use Speakap\SDK\Exception\InvalidSignatureException;

class SignedRequest
{
    /**
     * The default window a request is valid, in seconds
     */
    const DEFAULT_WINDOW = 60;

    /**
     * A RFC3986 encoded string
     * @var string
     */
    private $payload;

    /**
     * The query string as elements
     * @var array
     */
    private $decodedPayload;

    /**
     * @var string
     */
    private $appId;

    /**
     * The shared secret
     * @var string
     */
    private $appSecret;

    /**
     * The time window in seconds
     * @var int
     */
    private $signatureWindowSize;

    /**
     * @param string $appId
     * @param string $appSecret
     * @param int    $signatureWindowSize Time in seconds that the window is considered valid
     */
    public function __construct($appId, $appSecret, $signatureWindowSize = null)
    {
        $this->appId = $appId;
        $this->appSecret = $appSecret;
        $this->signatureWindowSize = $signatureWindowSize === null ? static::DEFAULT_WINDOW : $signatureWindowSize;
    }

    /**
     * Set the raw payload. Typically used in conjunction with isValid()
     *
     * @param string $payload typically set via: file_get_contents('php://input');
     *
     * @throws \InvalidArgumentException
     * @return $this
     */
    public function setPayload($payload)
    {
        $payloadProperties = $this->decodePayloadToArray($payload);
        if ( ! $this->isValidPayload($payloadProperties)) {
            throw new \InvalidArgumentException('Missing payload properties, got: '. print_r($payloadProperties, true));
        }

        $this->payload = $payload;
        $this->decodedPayload = $payloadProperties;

        return $this;
    }

    /**
     * @param array $params
     *
     * @throws \InvalidArgumentException
     *
     * @return bool
     */
    public function validateSignature(array $params)
    {
        if ( ! $this->isValidPayload($params)) {
            throw new \InvalidArgumentException('Missing payload properties, got: '. print_r($params, true));
        }

        if ($params['signature'] !== $this->getSignatureFromParameters($this->appSecret, $params)) {
            throw new InvalidSignatureException('Invalid signature, got: '. print_r($params, true));
        }

        $issuedAt = ExtendedDateTime::createFromFormat(\DateTime::ISO8601, $params['issuedAt']);
        if ( ! $this->isWithinWindow($this->signatureWindowSize, $issuedAt)) {
            throw new ExpiredSignatureException('Expired signature, got: '. print_r($params, true));
        }

        return true;
    }

    /**
     * Whether or not the payload is valid
     *
     * @return boolean
     */
    public function isValid()
    {
        if ($this->payload !== $this->getSelfSignedRequest($this->appSecret, $this->decodedPayload)) {
            // The payload doesn't match
            return false;
        }

        $issuedAt = ExtendedDateTime::createFromFormat(\DateTime::ISO8601, $this->decodedPayload['issuedAt']);
        if ( ! $this->isWithinWindow($this->signatureWindowSize, $issuedAt)) {
            // The date of the request does not fall in the allowed window size.
            return false;
        }

        return true;
    }

    /**
     * This method returns an encoded signed request. It does not re-use the original input,
     * but instead signs and encodes the properties.
     *
     * @return string
     */
    public function __toString()
    {
        return $this->getSelfSignedRequest($this->appSecret, $this->decodedPayload);
    }

    /**
     * Whether or not the request is within a sane window.
     *
     * @param integer   $signatureWindowSize
     * @param \DateTime $issuedAt
     *
     * @throws \InvalidArgumentException
     *
     * @return boolean
     */
    protected function isWithinWindow($signatureWindowSize, \DateTime $issuedAt)
    {
        if (! $issuedAt instanceof ExtendedDateTime) {
            throw new \InvalidArgumentException('Invalid timestamp supplied.');
        }

        $now = new ExtendedDateTime();

        $diff = $now->getTimestamp() - $issuedAt->getTimestamp();

        // The diff must be less than, or equal to the window size. To protect against overflow possibilities
        // we test if the differences is equal to, or greater than 0.
        if ($diff <= $signatureWindowSize && $diff >= 0) {
            return true;
        }

        return false;
    }

    /**
     * Sign the remote payload with the local (shared) secret. The result should be identical to the one we got
     * from the server.
     *
     * @param string $secret
     * @param array  $requestParameters
     *
     * @return string
     */
    protected function getSelfSignedRequest($secret, array $requestParameters)
    {
        $requestParameters['signature'] = $this->getSignatureFromParameters($secret, $requestParameters);

        return http_build_query($requestParameters);
    }

    /**
     * Generate the signature, based on the request parameters
     *
     * @param string $secret
     * @param array  $requestParameters
     *
     * @return string
     */
    protected function getSignatureFromParameters($secret, array $requestParameters)
    {
        unset($requestParameters['signature']);

        return base64_encode(
            hash_hmac(
                'sha256',
                http_build_query($requestParameters),
                $secret,
                false
            )
        );
    }

    /**
     * Validate the existence of the payload properties.
     *
     * @param array $payloadProperties
     *
     * @return bool
     */
    protected function isValidPayload(array $payloadProperties)
    {
        $defaultPayload = array(
            'appData' => null,
            'issuedAt' => null,
            'locale' => null,
            'networkEID' => null,
            'userEID' => null,
            'signature' => null
        );

        return count($defaultPayload) <= count(array_intersect_key($defaultPayload, $payloadProperties));
    }

    /**
     * Decode the payload string and convert it to an associative array.
     *
     * @param string $payload
     *
     * @return array
     */
    protected function decodePayloadToArray($payload)
    {
        parse_str($payload, $payloadProperties);

        return $payloadProperties;
    }

    /**
     * Decodes a RFC3986 encoded signed request payload
     *
     * @param string $payload
     *
     * @return string
     */
    protected function decodePayload($payload)
    {
        return urldecode($payload);
    }

    /**
     * Encode a string conform RFC3986.
     *
     * @param $payload
     *
     * @return string
     */
    protected function encodePayload($payload)
    {
        return urlencode($payload);
    }
}
