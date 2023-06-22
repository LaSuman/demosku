using System;
using System.Collections.Generic;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.RegularExpressions;

namespace test1
{
    public class DictionaryMapping
    {
        public void CreateMap()
        {

            // Get the file path from the command-line argument
            string xmlFilePath = @"C:\Users\icspcs\Desktop\webApp\test1\test1\b.xml";

            // Read the XML content from the file
            string xmlContent = File.ReadAllText(xmlFilePath);


            string filePath = @"C:\Users\icspcs\Desktop\webApp\test1\test1\a.txt";
            string SKUContents = File.ReadAllText(filePath);


            // Create a dictionary to store SKU-category
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            string[] lines = SKUContents.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                string[] keyValue = line.Split(' ');

                if (keyValue.Length != 2)
                {
                    string key = keyValue[0].Trim();
                    string value = "";

                    dictionary[key] = value;
                }
            }


            Dictionary<string, List<(Regex Pattern, double Authority)>> skuCategoryMap = new Dictionary<string, List<(Regex, double)>>();
            string matchingContent = @"<category\s+id=""(?<id>\d+)""\s+name=""(?<name>[^""]+)""\s+primary=""(?<primary>[^""]+)""\s+authority=""(?<authority>[^""]+)""\s*>";
            MatchCollection categoryMatches = Regex.Matches(xmlContent, matchingContent);
            foreach (Match categoryMatch in categoryMatches)
            {
                string categoryName = categoryMatch.Groups[2].Value;
                string primaryAttribute = categoryMatch.Groups[3].Value;
                string authorityAttribute = categoryMatch.Groups[4].Success ? categoryMatch.Groups[4].Value : "5.0";

                // Check if the category name starts with "* "
                if (categoryName.StartsWith("* "))
                {
                    double authority = double.Parse(authorityAttribute) - 2.5;
                    authorityAttribute = authority.ToString();
                }

                // Split primary attribute by comma and create a regex pattern for each value
                string[] primaryRegexes = primaryAttribute.Split(',');
                List<(Regex, double)> patterns = new List<(Regex, double)>();
                foreach (string regex in primaryRegexes)
                {
                    string pattern = $"^{regex.Trim()}"; // Anchor the pattern to the start of the string
                    patterns.Add((new Regex(pattern), double.Parse(authorityAttribute)));
                }

                // Add SKU-category mappings to the dictionary
                skuCategoryMap.Add(categoryName, patterns);
            }

        
            // Create a string to store the CSV content
            string csvContent = "SKU,category_id" + Environment.NewLine;

            // Determine the primary category with highest authority for each SKU and add it to the CSV content
            foreach (string sku in lines)
            {
                if (sku.StartsWith("SHIP"))
                    continue;

                double highestAuthority = 0.0;
                string highestCategory = null;

                foreach (var kvp in skuCategoryMap)
                {
                    string categoryName = kvp.Key;
                    List<(Regex, double)> patterns = kvp.Value;

                    foreach ((Regex pattern, double authority) in patterns)
                    {
                        if (pattern.IsMatch(sku) && authority > highestAuthority)
                        {
                            highestAuthority = authority;
                            highestCategory = categoryName;
                        }
                    }
                }

                if (highestCategory != null)
                {
                    csvContent += $"{sku},{highestCategory}" + Environment.NewLine;
                }
            }

            // Check write permissions in the current directory
            string currentDirectory = Environment.CurrentDirectory;
            bool hasWriteAccess = HasWritePermissions(currentDirectory);

            if (hasWriteAccess)
            {
                // Write the CSV content to a file
                File.WriteAllText(Path.Combine(currentDirectory, "sku_categories.csv"), csvContent);

                Console.WriteLine("CSV file generated successfully!");
            }
            else
            {
                Console.WriteLine("Unable to create the CSV file. Insufficient write permissions in the current directory.");
            }
        }

        // Helper method to check write permissions in a directory
        static bool HasWritePermissions(string directoryPath)
        {
            try
            {
                // Get the directory security information
                DirectorySecurity directorySecurity = Directory.GetAccessControl(directoryPath);

                // Check if the current user has write access
                AuthorizationRuleCollection authorizationRules = directorySecurity.GetAccessRules(true, true, typeof(SecurityIdentifier));
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                SecurityIdentifier userSid = identity.User;
                foreach (FileSystemAccessRule rule in authorizationRules)
                {
                    if (rule.IdentityReference == userSid && (rule.FileSystemRights & FileSystemRights.Write) == FileSystemRights.Write)
                    {
                        return true;
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("Unable to retrieve directory security information");
            }

            return false;
        }
    }
}