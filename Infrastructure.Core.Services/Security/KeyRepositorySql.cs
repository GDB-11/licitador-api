namespace Infrastructure.Core.Services.Security;

public static class KeyRepositorySql
{
    public const string Insert = """
                                 INSERT INTO "Security"."KeyPair"
                                     ("Id", "PublicKey", "PrivateKey", "IsActive", "ExpiresAt")
                                 VALUES
                                     (@Id, @PublicKey, @PrivateKey, @IsActive, @ExpiresAt)
                                 """;
    
    public const string Deactivate = """
                              UPDATE "Security"."KeyPair"
                              SET "IsActive" = false
                                  , "UsedAt" = (NOW() AT TIME ZONE 'UTC')
                              WHERE "Id" = @Id
                              """;
    
    public const string GetById = """
                                  SELECT
                                      "Id", "PublicKey", "PrivateKey", "IsActive",
                                      "CreatedAt", "ExpiresAt", "UsedAt"
                                  FROM "Security"."KeyPair"
                                  WHERE "Id" = @KeyPairId
                                      AND "IsActive" = true
                                      AND "ExpiresAt" > (now()::timestamp)
                                  """;
}