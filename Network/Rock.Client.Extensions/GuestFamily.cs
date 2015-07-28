using System;
using System.Collections.Generic;

namespace Rock.Client
{
    public class GuestFamily
    {
        public class Member
        {
            public int Id { get; set; }
            public int PersonAliasId { get; set; }
            public Guid Guid { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string PhotoUrl { get; set; }
            public bool CanCheckin { get; set; }
        }
        
        public int Id { get; set; }
        public string Name { get; set; }
        public Guid Guid { get; set; }

        public List<Member> FamilyMembers { get; set; }

        public GuestFamily( )
        {
        }

        public void SortMembers( )
        {
            // sort the family members by adult / child
            FamilyMembers.Sort( delegate( Member x, Member y )
                {
                    // get their birthdays
                    /*DateTime xDate = x.Person.BirthDate.HasValue ? x.Person.BirthDate.Value : DateTime.MinValue;
                    DateTime yDate = y.Person.BirthDate.HasValue ? y.Person.BirthDate.Value : DateTime.MinValue;

                    if( xDate < yDate )
                    {
                        return -1;
                    }*/

                    return 1;
                } );
        }
    }
}

