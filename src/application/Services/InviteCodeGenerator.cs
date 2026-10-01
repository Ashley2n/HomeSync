using System.Security.Cryptography;
using application.Interface;

namespace application.Services;

public class InviteCodeGenerator : IInviteCodeGenerator
{
    public const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
    public const int Length = 8;
    public string Generate() => 
        new(RandomNumberGenerator.GetItems<char>(Alphabet,Length));
}