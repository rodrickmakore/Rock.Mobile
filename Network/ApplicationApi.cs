﻿using System;
using Rock.Mobile.Network;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Rock.Mobile.Network
{
    /// <summary>
    /// Layer that lies on top of / wraps RockApi. This uses RockApi
    /// to implement application relevant methods.
    /// Example: Implements a method to add / update a Person using the low-level Post/Put People endpoint.
    /// </summary>
    public static class ApplicationApi
    {
        /// <summary>
        /// Wrapper for the ResolvePersonAliasId( int ), which will use a person object and modify the Id in person object on
        /// a successful return.
        /// </summary>
        public delegate void OnPersonAliasIdResolved( int personId );
        public static void ResolvePersonAliasId( Rock.Client.Person person, OnPersonAliasIdResolved onComplete )
        {
            // make the request for the ID
            if ( person.PrimaryAliasId.HasValue == true )
            {
                RockApi.Get_PersonAliasIdToPersonId( person.PrimaryAliasId.Value, 
                    delegate(HttpStatusCode statusCode, string statusDescription, RockApi.PersonIdObj model)
                    {
                        if( Rock.Mobile.Network.Util.StatusInSuccessRange( statusCode ) )
                        {
                            // sync the new person ID.
                            person.Id = model.PersonId;
                            onComplete( model.PersonId );
                        }
                        else
                        {
                            onComplete( -1 );
                        }

                    } );
            }
            else
            {
                onComplete( person.Id );
            }
        }

        public static void ResolvePersonAliasId( int personAliasId, OnPersonAliasIdResolved onComplete )
        {
            // make the request for the ID
            RockApi.Get_PersonAliasIdToPersonId( personAliasId, 
                delegate(HttpStatusCode statusCode, string statusDescription, RockApi.PersonIdObj model)
                {
                    if( Rock.Mobile.Network.Util.StatusInSuccessRange( statusCode ) )
                    {
                        onComplete( model.PersonId );
                    }
                    else
                    {
                        onComplete( -1 );
                    }
                } );
        }

        public static void UpdateOrAddPerson( Rock.Client.Person person, bool isNew, HttpRequest.RequestResult resultHandler )
        {
            if ( isNew == true )
            {
                person.Guid = Guid.NewGuid( );
                RockApi.Post_People( person, resultHandler );
            }
            else
            {
                RockApi.Put_People( person, resultHandler );
            }
        }

        public static void GetPersonByGuid( Guid guid, HttpRequest.RequestResult<Rock.Client.Person> resultHandler )
        {
            string oDataFilter = string.Format( "?$filter=Guid eq guid'{0}'", guid.ToString( ) );
            RockApi.Get_People<List<Rock.Client.Person>>( oDataFilter, 
                delegate(HttpStatusCode statusCode, string statusDescription, List<Rock.Client.Person> personList) 
                {
                    Rock.Client.Person returnPerson = null;

                    if( Rock.Mobile.Network.Util.StatusInSuccessRange( statusCode ) == true )
                    {
                        returnPerson = personList[ 0 ];
                    }

                    resultHandler( statusCode, statusDescription, returnPerson );
                });
        }

        public static void GetPersonById( int id, bool loadAttributes, HttpRequest.RequestResult<Rock.Client.Person> resultHandler )
        {
            string oDataFilter = "/" + id.ToString( );
            if ( loadAttributes == true )
            {
                oDataFilter += "?LoadAttributes=simple";
            }

            RockApi.Get_People<Rock.Client.Person>( oDataFilter, resultHandler );
        }

        const int CellPhoneValueId = 12;
        public static void AddOrUpdateCellPhoneNumber( Rock.Client.Person person, Rock.Client.PhoneNumber phoneNumber, bool isNew, HttpRequest.RequestResult resultHandler )
        {
            // first, get the latest person ID
            ResolvePersonAliasId( person, 
                delegate(int personId )
                {
                    // update the phone number
                    phoneNumber.PersonId = personId;
                    phoneNumber.NumberTypeValueId = CellPhoneValueId;

                    // now we can upload it.
                    if ( isNew )
                    {
                        // set the required values for a new phone number
                        phoneNumber.Guid = Guid.NewGuid( );
                        RockApi.Post_PhoneNumbers( phoneNumber, resultHandler );
                    }
                    else
                    {
                        // or just update the existing number
                        RockApi.Put_PhoneNumbers( phoneNumber, resultHandler );
                    }
                } );
        }

        public static void UpdateHomeCampus( Rock.Client.Group familyGroup, HttpRequest.RequestResult resultHandler )
        {
            // We can use UpdateFamily to update the campus
            UpdateFamilyGroup( familyGroup, resultHandler );
        }

        public static void UpdateFamilyGroup( Rock.Client.Group familyGroup, HttpRequest.RequestResult resultHandler )
        {
            // This updates the family to match whatever was passed in as familyGroup.
            // It can be used to update any aspect of a family, including Campus, Name, etc.
            Rock.Client.Group updatedGroup = new Rock.Client.Group();
            updatedGroup.Id = familyGroup.Id;
            updatedGroup.Guid = familyGroup.Guid;
            updatedGroup.IsSystem = familyGroup.IsSystem;
            updatedGroup.ParentGroupId = null;
            updatedGroup.GroupTypeId = familyGroup.GroupTypeId;
            updatedGroup.CampusId = familyGroup.CampusId;
            updatedGroup.ScheduleId = null;
            updatedGroup.Name = familyGroup.Name;
            updatedGroup.Description = familyGroup.Description;
            updatedGroup.IsSecurityRole = familyGroup.IsSecurityRole;
            updatedGroup.IsActive = familyGroup.IsActive;
            updatedGroup.Order = familyGroup.Order;
            updatedGroup.AllowGuests = familyGroup.AllowGuests;
            updatedGroup.GroupType = null;

            RockApi.Put_Groups( familyGroup, resultHandler );
        }

        public static void UpdateFamilyAddress( Rock.Client.Group family, Rock.Client.GroupLocation address, HttpRequest.RequestResult resultHandler )
        {
            RockApi.Put_Groups_SaveAddress( family, address, resultHandler );
        }

        public static void GetFamiliesOfPerson( Rock.Client.Person person, HttpRequest.RequestResult< List<Rock.Client.Group> > resultHandler )
        {
            ResolvePersonAliasId( person, delegate(int personId )
                {
                    string oDataFilter = "?$expand=GroupType,Campus,Members/GroupRole,GroupLocations/Location,GroupLocations/GroupLocationTypeValue,GroupLocations/Location/LocationTypeValue";

                    RockApi.Get_FamiliesOfPerson( personId, oDataFilter, resultHandler );
                } );
        }

        public static void GetFamilyGroupModelById( int familyGroupId, HttpRequest.RequestResult<Rock.Client.Group> resultHandler )
        {
            string oDataFilter = string.Format( "/{0}?LoadAttributes=simple", familyGroupId );
            RockApi.Get_Groups<Rock.Client.Group>( oDataFilter, resultHandler );
        }

        public static void UpdatePerson( Rock.Client.Person person, HttpRequest.RequestResult resultHandler )
        {
            // update the profile by the personID
            ResolvePersonAliasId( person, 
                delegate(int personId )
                {
                    RockApi.Put_People( person, resultHandler );
                } );
        }

        /// <summary>
        /// Given a "GroupTypeRole" guid, this will return the "GroupTypeRole" models.
        /// An example is a guid representing a Family Member, and it returns the GroupTypeRole of, say, a child.
        /// </summary>
        public static void GetGroupTypeRoleForGuid( string groupTypeRoleGuid, HttpRequest.RequestResult<List<Rock.Client.GroupTypeRole>> resultHandler )
        {
            string oDataFilter = string.Format( "?$filter=Guid eq guid'{0}'", groupTypeRoleGuid );

            RockApi.Get_GroupTypeRoles( oDataFilter, resultHandler );
        }

        public static void GetAttribute( int[] attributeIds, HttpRequest.RequestResult<List<Rock.Client.Attribute>> resultHandler )
        {
            string oDataFilter = "?$filter={0}&$expand=AttributeQualifiers,FieldType&$orderby=Id";

            // build the full list of attributes to request. Start with the first
            string attribIdsString = string.Format( "Id eq {0}", attributeIds[ 0 ] );

            // and 'or' in any additional
            for ( int i = 1; i < attributeIds.Length; i++ )
            {
                attribIdsString += string.Format( " or Id eq {0}", attributeIds[ i ] );
            }

            string oDataFullFilter = string.Format( oDataFilter, attribIdsString );

            RockApi.Get_Attributes( oDataFullFilter, resultHandler );
        }

        public static void UploadSavedProfilePicture( Rock.Client.Person person, MemoryStream imageStream, HttpRequest.RequestResult result )
        {
            // verify it's valid and not corrupt, or otherwise unable to load. If it is, we'll stop here.
            if ( imageStream != null )
            {
                // this is a big process. The profile picture being updated also requires the user's
                // profile be updated AND they need to be placed into a special group.
                // So, until ALL THOSE succeed in order, we will not consider the profile image "clean"


                // attempt to upload it
                UploadPersonPicture( imageStream, 

                    delegate( System.Net.HttpStatusCode statusCode, string statusDesc, int photoId )
                    {
                        // if the upload went ok
                        if ( Rock.Mobile.Network.Util.StatusInSuccessRange( statusCode ) == true )
                        {
                            // now update the profile
                            person.PhotoId = photoId;

                            // attempt to sync the profile
                            UpdateOrAddPerson( person, false,
                                delegate ( System.Net.HttpStatusCode profileStatusCode, string profileStatusDesc )
                                {
                                    if ( Rock.Mobile.Network.Util.StatusInSuccessRange( profileStatusCode ) == true )
                                    {
                                        // now (and only now) that we know the profile was updated correctly,
                                        // we can update the image group.
                                        UpdatePersonImageGroup( person, delegate ( System.Net.HttpStatusCode resultCode, string resultDesc )
                                            {
                                                if ( result != null )
                                                {
                                                    result( statusCode, statusDesc );
                                                }
                                            } );
                                    }
                                    else
                                    {
                                        if ( result != null )
                                        {
                                            result( statusCode, statusDesc );
                                        }
                                    }
                                } );
                        }
                        else
                        {
                            if ( result != null )
                            {
                                result( statusCode, statusDesc );
                            }
                        }
                    } );
            }
            else
            {
                // the picture failed to save
                result( System.Net.HttpStatusCode.BadRequest, "" );
            }
        }

        const string FileTypeImageGuid = "03BD8476-8A9F-4078-B628-5B538F967AFC";
        class ImageResponse
        {
            public string Id { get; set; }
            public string FileName { get; set; }
        }
        public static void UploadPersonPicture( MemoryStream imageBuffer, HttpRequest.RequestResult<int> resultHandler )
        {
            // send up the image for the user
            RockApi.Post_FileUploader( imageBuffer, true, FileTypeImageGuid, false, 

                delegate(HttpStatusCode statusCode, string statusDescription, byte[] responseBytes )
                {
                    if( Rock.Mobile.Network.Util.StatusInSuccessRange( statusCode ) == true )
                    {
                        // deserialize the raw response into our wrapper class
                        ImageResponse imageResponse = JsonConvert.DeserializeObject<ImageResponse>( System.Text.Encoding.ASCII.GetString( responseBytes ) );

                        // now call the final result
                        resultHandler( statusCode, statusDescription, int.Parse( imageResponse.Id ) );
                    }
                    else
                    {
                        resultHandler( statusCode, statusDescription, 0 );
                    }
                } );
        }

        const int GroupMemberRole_Member_ValueId = 59;
        const int ApplicationGroup_PhotoRequest_ValueId = 1207885;
        const Rock.Client.Enums.GroupMemberStatus GroupMemberStatus_Pending_ValueId = Rock.Client.Enums.GroupMemberStatus.Pending;
        public static void UpdatePersonImageGroup( Rock.Client.Person person, HttpRequest.RequestResult resultHandler )
        {
            ResolvePersonAliasId( person, 
                delegate(int personId )
                {
                    string oDataFilter = string.Format( "?$filter=PersonId eq {0} and GroupId eq {1}", personId, ApplicationGroup_PhotoRequest_ValueId );
                    RockApi.Get_GroupMembers( oDataFilter, delegate(HttpStatusCode statusCode, string statusDescription, List<Rock.Client.GroupMember> model )
                        {
                            if ( Rock.Mobile.Network.Util.StatusInSuccessRange( statusCode ) == true )
                            {
                                // if it's null, they are NOT in the group and we should POST. if it's valid,
                                // we can simply update the existing.
                                if ( model.Count == 0 )
                                {
                                    Rock.Client.GroupMember groupMember = new Rock.Client.GroupMember();
                                    groupMember.Guid = Guid.NewGuid( );
                                    groupMember.PersonId = personId;
                                    groupMember.GroupMemberStatus = GroupMemberStatus_Pending_ValueId;
                                    groupMember.GroupId = ApplicationGroup_PhotoRequest_ValueId;
                                    groupMember.GroupRoleId = GroupMemberRole_Member_ValueId;

                                    RockApi.Post_GroupMembers( groupMember, resultHandler );
                                }
                                else
                                {
                                    // otherwise, we'll do a PUT
                                    Rock.Client.GroupMember groupMember = new Rock.Client.GroupMember();

                                    // set the status to pending
                                    groupMember.GroupMemberStatus = GroupMemberStatus_Pending_ValueId;

                                    // and copy over all the other data
                                    groupMember.PersonId = model[ 0 ].PersonId;
                                    groupMember.Guid = model[ 0 ].Guid;
                                    groupMember.GroupId = model[ 0 ].GroupId;
                                    groupMember.GroupRoleId = model[ 0 ].GroupRoleId;
                                    groupMember.Id = model[ 0 ].Id;
                                    groupMember.IsSystem = model[ 0 ].IsSystem;

                                    RockApi.Put_GroupMembers( groupMember, resultHandler );
                                }
                            }
                            else
                            {
                                // fail...
                                resultHandler( statusCode, statusDescription );
                            }

                        } );
                } );
        }

        /// <summary>
        /// Given a "DefinedType" guid, this will return that "DefinedType"s id.
        /// The Id can then be used to get the "DefinedValues" for this "DefinedType".
        /// </summary>
        public static void GetDefinedTypeIdForGuid( string definedTypeGuid, HttpRequest.RequestResult<List<Rock.Client.DefinedType>> resultHandler )
        {
            string oDataFilter = string.Format( "?$filter=Guid eq guid'{0}'", definedTypeGuid );

            RockApi.Get_DefinedTypes( oDataFilter, resultHandler );
        }

        /// <summary>
        /// Defined Values are the "Values" of Defined Types.
        /// Example, Marital Status is a Defined Type.
        /// Its Values are "Married" and "Single".
        /// Starting with a GUID representing the DefinedType, we get the DefinedType's ID
        /// which can then be used to query the values.
        /// </summary>
        public static void GetDefinedValuesForDefinedType( string definedTypeGuid, HttpRequest.RequestResult<List<Rock.Client.DefinedValue>> resultHandler )
        {
            GetDefinedTypeIdForGuid( definedTypeGuid,
                delegate(HttpStatusCode statusCode, string statusDescription, List<Rock.Client.DefinedType> model )
                {
                    if ( model != null && model.Count > 0 )
                    {
                        string oDataFilter = string.Format( "?LoadAttributes=simple&$filter=DefinedTypeId eq {0}", model[ 0 ].Id );

                        // it worked, so now use its Id to get the defined VALUE, which is what we really want.
                        RockApi.Get_DefinedValues( oDataFilter, resultHandler );
                    }
                    else
                    {
                        resultHandler( HttpStatusCode.NotFound, "", null );
                    }
                } );
        }
    }
}
