using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Rock.Client
{
    public class Family
    {
        [JsonProperty]
        public int Id { get; set; }

        [JsonProperty]
        public string Name { get; set; }

        [JsonProperty]
        public Rock.Client.Location HomeLocation { get; set; }

        [JsonProperty]
        public List<Rock.Client.GroupMember> FamilyMembers { get; set; }

        [JsonProperty]
        public Rock.Client.PhoneNumber MainPhoneNumber { get; set; }

        public Family( )
        {
            FamilyMembers = new List<Rock.Client.GroupMember>( );
            HomeLocation = new Rock.Client.Location();
            MainPhoneNumber = new Rock.Client.PhoneNumber();
        }

        public void SortMembers( )
        {
            // sort the family members by adult / child
            FamilyMembers.Sort( delegate(Rock.Client.GroupMember x, Rock.Client.GroupMember y )
                {
                    // get their birthdays
                    DateTime xDate = x.Person.BirthDate.HasValue ? x.Person.BirthDate.Value : DateTime.MinValue;
                    DateTime yDate = y.Person.BirthDate.HasValue ? y.Person.BirthDate.Value : DateTime.MinValue;

                    if( xDate < yDate )
                    {
                        return -1;
                    }

                    return 1;
                } );
        }
    }
}

