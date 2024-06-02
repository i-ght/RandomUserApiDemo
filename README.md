# Objective

Create a C# Console application that can request users from the public “Random User Generator API”, available at: https://randomuser.me/documentation , manipulate and save the data as a CSV onto the Drive.

# Requirements:

1. You must use the default format (JSON) from the “Random User Generator API”.
2. You must use the Seed “Goal” for the “Random User Generator API”.
3. The CSV should contain 100 users, of which exactly 50 should be marked as Female.
4. The CSV should contain users with at least 4 different Nationalities with no Nationality
representing more than 40% of the users.
5. Calculate the Age of each user from their DOB and include it in the CSV.
6. Generate a unique 7-digit Id for each user to be included in the CSV.
7. The CSV File Generated must contain the following columns: ID, FirstName, LastName, Gender,
Email, Username, DOB (Date of Birth), and Age.
8. Gender Must be either a 0, 1, or 2 representing Male, Female, or Other.
9. DOB must be in the following format: YYYY/MM/DD
10. The CSV File name should follow this format: (YourLastName_YourFirstName)_users.csv