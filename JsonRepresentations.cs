using System.Text.Json.Serialization;

namespace RandomUserApiDemo;


/* RandomUser API JSON objects represented as C# records below */

public record RandUserResult(
    [property: JsonRequired] List<UserInfoIn> Results,
    Info Info
);

public record Coordinates(string Latitude, string Longitude);

public record Timezone(string Offset, string Description);

public record Street(int Number, string Name);

public record Location(
    Street Street,
    string City,
    string State,
    [property: JsonRequired] string Country,
    Coordinates Coordinates,
    Timezone Timezone
);

public record UserInfoIn(
    [property: JsonRequired] string Gender,
    [property: JsonRequired] Name Name,
    [property: JsonRequired] Location Location,
    [property: JsonRequired] string Email,
    [property: JsonRequired] Login Login,
    [property: JsonRequired] DateOfBirth Dob
);

public record Login(
    string Uuid,
    [property: JsonRequired] string Username,
    string Password,
    string Salt,
    string Md5,
    string Sha1,
    string Sha256
);

public record Name(
    string Title,
    [property: JsonRequired] string First,
    [property: JsonRequired] string Last
);

public record DateOfBirth(
    DateTimeOffset Date,
    int Age
);

public record Info(
    string Seed,
    int Results,
    int Page,
    string Version
);