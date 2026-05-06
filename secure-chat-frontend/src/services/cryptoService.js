import CryptoJS from 'crypto-js';
import JSEncrypt from 'jsencrypt';

const formatAsPem = (base64Key, type) => {
    const cleanKey = base64Key.replace(/\s/g, '');
    const matched = cleanKey.match(/.{1,64}/g);
    if (!matched) return '';
    return `-----BEGIN ${type} KEY-----\n${matched.join('\n')}\n-----END ${type} KEY-----`;
};

export const cryptoService = {
    
    generateRSAKeyPair: async () => {
        const keyPair = await window.crypto.subtle.generateKey(
            {
                name: "RSA-OAEP",
                modulusLength: 2048,
                publicExponent: new Uint8Array([1, 0, 1]),
                hash: "SHA-256",
            },
            true, 
            ["encrypt", "decrypt"]
        );

        const exportedPublic = await window.crypto.subtle.exportKey("spki", keyPair.publicKey);
        const publicKeyBase64 = btoa(String.fromCharCode(...new Uint8Array(exportedPublic)));

        const exportedPrivate = await window.crypto.subtle.exportKey("pkcs8", keyPair.privateKey);
        const privateKeyBase64 = btoa(String.fromCharCode(...new Uint8Array(exportedPrivate)));

        return {
            publicKey: publicKeyBase64,
            privateKey: privateKeyBase64
        };
    },

    generateAesKey: () => {
        return CryptoJS.lib.WordArray.random(256 / 8).toString(CryptoJS.enc.Base64);
    },

    encryptMessageWithAes: (plainText, aesKeyBase64) => {
        const aesKeyHex = CryptoJS.enc.Base64.parse(aesKeyBase64);
        const iv = CryptoJS.lib.WordArray.random(128 / 8); 
        
        const encrypted = CryptoJS.AES.encrypt(plainText, aesKeyHex, {
            iv: iv,
            mode: CryptoJS.mode.CBC,
            padding: CryptoJS.pad.Pkcs7
        });
        
        return iv.concat(encrypted.ciphertext).toString(CryptoJS.enc.Base64);
    },

    decryptMessageWithAes: (cipherTextBase64, aesKeyBase64) => {
        const aesKeyHex = CryptoJS.enc.Base64.parse(aesKeyBase64);
        const fullCipherHex = CryptoJS.enc.Base64.parse(cipherTextBase64);
        
        const iv = CryptoJS.lib.WordArray.create(fullCipherHex.words.slice(0, 4));
        const ciphertext = CryptoJS.lib.WordArray.create(fullCipherHex.words.slice(4));

        const decrypted = CryptoJS.AES.decrypt({ ciphertext: ciphertext }, aesKeyHex, {
            iv: iv,
            mode: CryptoJS.mode.CBC,
            padding: CryptoJS.pad.Pkcs7
        });
        
        return decrypted.toString(CryptoJS.enc.Utf8);
    },

    encryptAesKeyWithRsa: (aesKeyBase64, serverPublicKey) => {
        const pemKey = formatAsPem(serverPublicKey, "PUBLIC");
        const encryptor = new JSEncrypt();
        encryptor.setPublicKey(pemKey);
        return encryptor.encrypt(aesKeyBase64);
    },

    decryptAesKeyWithRsa: (encryptedAesKey, userPrivateKey) => {
        const pemKey = formatAsPem(userPrivateKey, "PRIVATE"); 
        const decryptor = new JSEncrypt();
        decryptor.setPrivateKey(pemKey);
        return decryptor.decrypt(encryptedAesKey);
    }
};