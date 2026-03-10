namespace MedicalDiagnosticSystem.MedicalDiagnostic.Data.Services;

public interface IEncryptionService
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}