using Org.BouncyCastle.Asn1.Nist;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;

namespace HomeM8Service.Utility
{
    public class ECDHKeyExchange
    {
        #region Ctor

        public ECDHKeyExchange()
        {
            GenerateOwnKey();
        }

        #endregion

        #region Private Members

        string ClientPublicKeyXmlString { get; set; }

        AsymmetricCipherKeyPair KeyPair { get; set; }

        X9ECParameters x9EC { get; set; }

        #endregion

        #region Response Methods

        public string GetPublicKeyXmlString()
        {
            return GetXmlString((ECPublicKeyParameters)KeyPair.Public);
        }

        public void SetClientXml(string response)
        {
            ClientPublicKeyXmlString = response;
        }

        #endregion

        #region Main Process
    
        void GenerateOwnKey()
        {
            x9EC = NistNamedCurves.GetByName("P-521");
            ECDomainParameters ecDomain = new ECDomainParameters(x9EC.Curve, x9EC.G, x9EC.N, x9EC.H, x9EC.GetSeed());
            AsymmetricCipherKeyPair aliceKeyPair = GenerateKeyPair(ecDomain);
            KeyPair = aliceKeyPair;
        }

        public byte[] GenerateSharedSecret()
        {
            ECPublicKeyParameters bobPublicKey = GetBobPublicKey(x9EC);

            byte[] AESKey = GenerateAESKey(bobPublicKey, KeyPair.Private);

            return AESKey;
        }

        private byte[] GenerateAESKey(ECPublicKeyParameters bobPublicKey, AsymmetricKeyParameter alicePrivateKey)
        {
            IBasicAgreement aKeyAgree = AgreementUtilities.GetBasicAgreement("ECDH");
            aKeyAgree.Init(alicePrivateKey);
            BigInteger sharedSecret = aKeyAgree.CalculateAgreement(bobPublicKey);
            byte[] sharedSecretBytes = sharedSecret.ToByteArray();

            IDigest digest = new Sha256Digest();
            byte[] symmetricKey = new byte[digest.GetDigestSize()];
            digest.BlockUpdate(sharedSecretBytes, 0, sharedSecretBytes.Length);
            digest.DoFinal(symmetricKey, 0);

            return symmetricKey;
        }

        private ECPublicKeyParameters GetBobPublicKey(X9ECParameters x9EC)
        {
            KeyCoords bobCoords = GetBobCoords();
            var point = x9EC.Curve.CreatePoint(bobCoords.X, bobCoords.Y);
            return new ECPublicKeyParameters("ECDH", point, SecObjectIdentifiers.SecP521r1);
        }

        private KeyCoords GetBobCoords()
        {
            if (ClientPublicKeyXmlString == null) throw new InvalidOperationException();

            string responseXml = ClientPublicKeyXmlString;

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(responseXml);
            XmlElement root = doc.DocumentElement;
            XmlNodeList elemList = doc.DocumentElement.GetElementsByTagName("PublicKey");

            return new KeyCoords
            {
                X = new BigInteger(elemList[0].FirstChild.Attributes["Value"].Value),
                Y = new BigInteger(elemList[0].LastChild.Attributes["Value"].Value)
            };
        }

        private string GetXmlString(ECPublicKeyParameters publicKeyParameters)
        {
            string publicKeyXmlTemplate = @"<ECDHKeyValue xmlns=""http://www.w3.org/2001/04/xmldsig-more#""> <DomainParameters> <NamedCurve URN=""urn:oid:1.3.132.0.35"" /> </DomainParameters> <PublicKey> <X Value=""X_VALUE"" xsi:type=""PrimeFieldElemType"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" /> <Y Value=""Y_VALUE"" xsi:type=""PrimeFieldElemType"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" /> </PublicKey> </ECDHKeyValue>";
            string xml = publicKeyXmlTemplate;
            xml = xml.Replace("X_VALUE", publicKeyParameters.Q.AffineXCoord.ToBigInteger().ToString());
            xml = xml.Replace("Y_VALUE", publicKeyParameters.Q.AffineYCoord.ToBigInteger().ToString());
            return xml;
        }

        private AsymmetricCipherKeyPair GenerateKeyPair(ECDomainParameters ecDomain)
        {
            ECKeyPairGenerator g = (ECKeyPairGenerator)GeneratorUtilities.GetKeyPairGenerator("ECDH");
            g.Init(new ECKeyGenerationParameters(ecDomain, new SecureRandom()));

            AsymmetricCipherKeyPair aliceKeyPair = g.GenerateKeyPair();
            return aliceKeyPair;
        }

        internal class KeyCoords
        {
            public BigInteger X { get; set; }
            public BigInteger Y { get; set; }
        } 

        #endregion
    }
}