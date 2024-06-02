
using StringPair = (string name, string value);

using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Web;

using CsvHelper;
using CsvHelper.TypeConversion;


namespace RandomUserApiDemo;

internal static class Utils
{
    private static readonly Random Random;
    static Utils() => Random = new Random();

    public static int RandomInt(int max)
    {
        /* lock (Random) {
            return Random.Next(max);
        } */
        
        return Random.Next(max);
    }

    public static string UrlEncodePair(StringPair pair)
    {
        var (a, b) = pair;
        return $"{HttpUtility.UrlEncode(a)}={HttpUtility.UrlEncode(b)}";
    }

    public static string UrlEncodeQuerySeq(IEnumerable<StringPair> query) =>
        string.Join("&", query.Select(UrlEncodePair));

}

/* 7. The CSV File Generated must contain the following columns: ID, FirstName, LastName, Gender,
Email, Username, DOB (Date of Birth), and Age. */
public record UserInfo(
    string ID,
    string FirstName,
    string LastName,
    int Gender,
    string Email,
    string Username,
    DateTimeOffset DOB,
    int Age,
    string Country
);

/* write csv file EZ */
public static class Csv
{
    public static void WriteUsers(
        string path,
        IEnumerable<UserInfo> users)
    {
        using var writer = new StreamWriter(path);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

        /* 9. DOB must be in the following format: YYYY/MM/DD */
        var options = new TypeConverterOptions { Formats = ["yyyy/MM/dd"] };
        csv.Context.TypeConverterOptionsCache.AddOptions<DateTimeOffset>(options);
        csv.Context.TypeConverterOptionsCache.AddOptions<DateTimeOffset?>(options);

        csv.WriteRecords(users);
    }
}


/* interact with random user http api */
internal static class RandomUserApi
{   
    private static readonly HttpClient Client;
    static RandomUserApi() =>
        Client = new HttpClient(
            /* new HttpClientHandler {Proxy=new WebProxy("127.0.0.1:8080")} */
        );
    
    public static async Task<RandUserResult> RetrieveRandUsers(
        /* string gender, */
        string seed,
        IEnumerable<string> nationalities,
        int amount)
    {

        /* when seed and gender are both in the query, for some reason
           the api returns all genders. 

           if I comment out seed then the api returns only specified
           gender. 
           
           programmer error or API error? ¯\_(ツ)_/¯ */
        StringPair[] query = [
            /* ("gender", gender), */
            ("results", amount.ToString()),
            ("nat", string.Join(',', nationalities)),
            ("seed", seed)
        ];

        const string ApiUri = "https://randomuser.me/api";

        var uri = $"{ApiUri}/?{Utils.UrlEncodeQuerySeq(query)}";
        
        using var req = new HttpRequestMessage(HttpMethod.Get, uri);
        using var response = await Client.SendAsync(req);
        
        response.EnsureSuccessStatusCode();

        var users = await response.Content.ReadFromJsonAsync<RandUserResult>();

        if (null == users) {
            throw new InvalidOperationException(
                "randomuser.me api returned unexpected null value."
            );
        }
        return users;
    }
}

internal static class Program
{
/* 6. Generate a unique 7-digit Id for each user to be included in the CSV.*/
    private static string SevenDigitRandomId()
    {
        const string digits = "123456789";
        var sb = new StringBuilder(capacity: 7);
        for (var i = 0; i < 7; i++) {
            sb.Append(digits[Utils.RandomInt(digits.Length)]);
        }
        return sb.ToString();
    }

/* 8. Gender Must be either a 0, 1, or 2 representing Male, Female, or Other.*/
    private static int GenderInt(string gender) {
        if (string.Equals(gender, "male", StringComparison.OrdinalIgnoreCase)) {
            return 0;
        } else if (string.Equals(gender, "female", StringComparison.OrdinalIgnoreCase)) {
            return 1;
        } else {
            return 2;
        }
    }

    private static Dictionary<string, List<UserInfoIn>> GroupByCountry(
        IEnumerable<UserInfoIn> users) 
    {
        var ret = new Dictionary<string, List<UserInfoIn>>();

        foreach (var user in users) {
            var key = user.Location.Country;
            if (!ret.ContainsKey(user.Location.Country)) {
                ret[key] = [];
            }

            ret[key].Add(user);
        }

        return ret;
    }

    private static List<UserInfoIn> Orchestrate(
        IEnumerable<UserInfoIn> users)
    {
        /* 3. The CSV should contain 100 users, of which exactly 50 should be marked as Female. */
        var men = users.Where(
            user =>
                user.Gender == "male"
        );
        var women = users.Where(
            user =>
                user.Gender == "female"
        );
        
        var menByCountry = GroupByCountry(men);
        var womenByCountry = GroupByCountry(women);

        /* Each dictionary contains 4 key values which are countries
           Each value is a sequence that contains around 100 users (if you called api with 999) */

        var organized = new List<UserInfoIn>(capacity: 104);
        foreach (var (_, value) in menByCountry) {
            /* 13*4=52. 13 users from each of the 4 unique countries gives what's needed */
            organized.AddRange(value.Take(13));
        }

        /* remove extra 2 for 50 total */        
        organized.RemoveRange(organized.Count - 3, 2);
        
        foreach (var (_, value) in womenByCountry) {
            organized.AddRange(value.Take(13));
        }

        /* remove extra 2 for 50 total */      
        organized.RemoveRange(organized.Count - 3, 2);
        

        return organized;
    }

    private static bool EnsureFourAnd40Percent(
        IEnumerable<UserInfoIn> users)
    {
        /*  4. The CSV should contain users with at least 4 different 
            Nationalities with no Nationality representing more than 40%
            of the users. */
        
        var usersList = users.ToList();
        
        /* count how many of each country there are */
        var nationalityCounts = new Dictionary<string, int>(capacity: 4);
        foreach (var user in usersList) {
            if (!nationalityCounts.TryGetValue(
                    user.Location.Country,
                    out int value
            )) {
                value = 0;
                nationalityCounts[user.Location.Country] = value;
            }
            nationalityCounts[user.Location.Country] = ++value;
        }

        if (nationalityCounts.Count < 4) {
            return false;
        }
        
        const double maxAllowedPercentage = 0.4;
        int maxAllowedCount = (int)(usersList.Count * maxAllowedPercentage);

        foreach (var (_, count) in nationalityCounts) {
            if (count > maxAllowedCount) {
                return false;
            }
        }
 
        return true;
    }

    public static List<UserInfo> Transform(
        IEnumerable<UserInfoIn> users
    ) {
        var xformedUsers = new List<UserInfo>(capacity: 100);

        var set = new HashSet<string>(100);
        while (set.Count < 100) {
            set.Add(SevenDigitRandomId());
        }
        var uniqueIds = new Queue<string>(set);

        foreach (var user in users) {
            var id = uniqueIds.Dequeue();

            var genderInt = GenderInt(user.Gender);

            /* Age is calculated by API and has value here user.Dob.Age 
               but instructions say: 
               5. Calculate the Age of each user from their DOB and include it in the CSV. */

            var today = DateTimeOffset.Now;
            var age = today.Year - user.Dob.Date.Year;
            if (user.Dob.Date > today.AddYears(-age)) {
                age--;
            }

            /* Debug.Assert(age == user.Dob.Age); */

            var xformed = new UserInfo(
                id,
                user.Name.First,
                user.Name.Last,
                genderInt,
                user.Email,
                user.Login.Username,
                user.Dob.Date,
                age,
                user.Location.Country
            );
            
            xformedUsers.Add(xformed);
        }


        return xformedUsers;
    }


    public static async Task Main()
    {
        /* 2. You must use the Seed “Goal” for the “Random User Generator API”. */
        const string seed = "Goal";

        string[] nationalities = [
            "us", "mx", "br", "ca"
        ];

        /*
            when seed and gender are both in the query, for some reason (programmer or API error?)
            the api returns all genders even though I have gender specified
            correctly, so we just call the API to give us more than enough
            users (999) and filter and organize accordingly
        */
        const int count = 999;

        var users = await RandomUserApi.RetrieveRandUsers(
            seed,
            nationalities,
            count
        );

        var organized = Orchestrate(users.Results);

        if (!EnsureFourAnd40Percent(organized)) {
            throw new InvalidOperationException(
                "Programmer error: need at least four nationalities and no more than 40% for each nationality."
            );
        }

        
        var xformed = Transform(organized);

        /* 10. The CSV File name should follow this format: (YourLastName_YourFirstName)_users.csv */
        const string path = "Wright_Christopher_users.csv";

        Csv.WriteUsers(path, xformed);
    }
}
