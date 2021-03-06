﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Apartrent_Try2
{
    public class DB
    {
        public static string CONN_STRING;


        public static class UsersDB
        {
            public static Users Login(Users user)
            {
                using (SqlConnection conn = new SqlConnection(CONN_STRING))
                {
                    conn.Open();
                    
                    if (ValidateUser(user.UserName, user.Password, conn))//check if the user is valide and if the password matches to the stored one
                    {
                        UpdateUserLastLogin(conn, user.UserName);//update user last login
                        return GetYourUserProfile(user.UserName, conn); //return an object of the user that include all the user data
                    }

                    return null;//if the validation is false the user object is null
                }
            }

            public static bool SignUp(Users user)
            {

                using (SqlConnection conn = new SqlConnection(CONN_STRING))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT UserName From Users WHERE UserName=@UserName", conn)) // check if user name is already exists
                    {
                        lock ("now") // locking the thread so the there would not be a situation that user get change on multithread enviorment
                        {
                            cmd.Add("@UserName", user.UserName); // add the user userName to the property @UserName
                            using (SqlDataReader dr = cmd.ExecuteReader())
                            {
                                if (dr.Read())
                                {
                                    return false;//user is already exist
                                }
                            }
                            cmd.CommandText = "INSERT INTO Users(UserName,Password,Gender,[Address],PhoneNumber,Email,FirstName,LastName,CountryID,Role) VALUES(@UserName,@Password,@Gender,@Address,@PhoneNumber,@Email,@FirstName,@LastName,@CountryID,@Role)";
                            cmd.Add("@Password", user.Password);
                            cmd.Add("@Gender", user.Gender);
                            cmd.Add("@Address", user.Address);
                            cmd.Add("@PhoneNumber", user.PhoneNumber);
                            cmd.Add("@Email", user.Email);
                            cmd.Add("@FirstName", user.FirstName);
                            cmd.Add("@LastName", user.LastName);
                            cmd.Add("@CountryID", user.CountryID);
                            cmd.Add("@Role", 0);
                            if (cmd.ExecuteNonQuery() == 1) // if the user as been register succesfully he can procced to image storing (if any image as been uploaded)
                            {

                                if (user.ProfileImage != null)
                                {
                                    cmd.CommandText = "INSERT INTO UsersProfileImage(UserName,Image,ImageType) VALUES(@UserName,@Image,@ImageType)";
                                    cmd.Add("@Image", user.ProfileImageByte);
                                    cmd.Add("@ImageType", user.ProfileImageType);
                                    return cmd.ExecuteNonQuery() == 1;
                                }
                                else
                                {
                                    cmd.CommandText = "INSERT INTO UsersProfileImage(UserName) VALUES(@UserName)";
                                    return cmd.ExecuteNonQuery() == 1;
                                }
                            }
                            return false;
                        }
                    }


                }
            }

            public static bool DeleteUser(string user)//deleteing the user data
            {
                using (SqlConnection conn = new SqlConnection(CONN_STRING))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("DELETE FROM Users WHERE UserName=@UserName", conn))
                    {
                        cmd.Add("@UserName", user);
                        return cmd.ExecuteNonQuery() == 1;
                    }
                }
            }

            public static bool EditUser(Users user)//edit the user without the image
            {
                using (SqlConnection conn = new SqlConnection(CONN_STRING))
                {
                    conn.Open();

                    using (SqlCommand cmd = new SqlCommand("UPDATE Users SET  Gender=@Gender,[Address]=@Address,PhoneNumber=@PhoneNumber,Email=@Email,FirstName=@FirstName,LastName=@LastName,CountryID=@CountryID WHERE UserName=@UserName", conn))
                    {
                        cmd.Add("@UserName", user.UserName);
                        cmd.Add("@Gender", user.Gender);
                        cmd.Add("@Address", user.Address);
                        cmd.Add("@PhoneNumber", user.PhoneNumber);
                        cmd.Add("@Email", user.Email);
                        cmd.Add("@FirstName", user.FirstName);
                        cmd.Add("@LastName", user.LastName);
                        cmd.Add("@CountryID", user.CountryID);
                        return cmd.ExecuteNonQuery() == 1;
                    }



                }
            }

            public static bool UpdateProfilePicture(Users user)//edit the user profile image
            {
                using (SqlConnection conn = new SqlConnection(CONN_STRING))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("UPDATE UsersProfileImage SET Image=@Image,ImageType=@ImageType WHERE UserName=@UserName", conn))
                    {
                        cmd.Add("@UserName", user.UserName);
                        cmd.Add("@Image", user.ProfileImageByte);
                        cmd.Add("@ImageType", user.ProfileImageType);
                        return cmd.ExecuteNonQuery() == 1;
                    }
                }
            }

            public static bool ValidateUser(string userName, string password, SqlConnection conn)
            {
                string storedHashPassword = null;//stored hash variable
                PasswordHash hash = new PasswordHash();//new hash class instence
                using (SqlCommand cmd = new SqlCommand("SELECT Password FROM Users WHERE UserName=@UserName", conn))
                {
                    cmd.Add("@UserName", userName);
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        if (dr.Read())
                        {
                            storedHashPassword = dr.GetString(0);//adding the stored password the the object
                        }

                    }
                    if (storedHashPassword != null)
                        return hash.Verify(password, storedHashPassword); //verify if the hash is correct
                    return false;

                }
            }


            public static void UpdateUserLastLogin(SqlConnection conn, string userName)//updating user last login
            {
                using (SqlCommand cmd = new SqlCommand("UPDATE Users SET LastLogin=@LastLogin WHERE UserName=@UserName", conn))
                {
                    cmd.Add("@LastLogin", DateTime.Now.Ticks);
                    cmd.Add("@UserName", userName);
                    cmd.ExecuteNonQuery();
                }
            }

            public static Users GetYourUserProfile(string userName, SqlConnection conn)//get user profile details
            {

                using (SqlCommand cmd = new SqlCommand("SELECT Gender,[Address], PhoneNumber, Email, FirstName, LastName, Users.CountryID AS CountryID, CountryName, Role, UsersProfileImage.Image AS Image,UsersProfileImage.ImageType as ImageType FROM Users INNER JOIN Countries ON Users.CountryID = Countries.CountryID INNER JOIN UsersProfileImage ON UsersProfileImage.UserName = Users.UserName WHERE Users.UserName = @UserName", conn))
                {
                    cmd.Add("@UserName", userName);
                    Users userDetails;
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        dr.Read();
                        userDetails = new Users()
                        {
                            UserName = userName,
                            Gender = dr.GetBoolean(0),
                            Address = dr.GetString(1),
                            PhoneNumber = dr.GetString(2),
                            Email = dr.GetString(3),
                            FirstName = dr.GetString(4),
                            LastName = dr.GetString(5),
                            CountryID = dr.GetInt32(6),
                            CountryName = dr.GetString(7),
                            Role = dr.GetInt32(8),
                            ProfileImageByte = (dr.IsDBNull(9) ? null : (byte[])dr.GetValue(9)),
                            ProfileImageType = (dr.IsDBNull(10) ? null : dr.GetString(10))
                        };

                    }
                    userDetails.ProfileImage = ImageValidation.BytesToBase64(userDetails.ProfileImageByte, null)[0];//converting the image if exists
                    userDetails.ProfileImageByte = null;
                    userDetails.Token = (string)AuthService.GetToken(userDetails.UserName, userDetails.Role);//creating new token for the user
                    if (userDetails.Role == (int)Role.User)//if user is not a renter return the object else get user apartments
                        return userDetails;
                    if (userDetails.Role == (int)Role.Renter) //if user is renter fetching all of his apartments data
                    {
                        //asking for apartment details,Joining the next tables - ApartmentImages,ApartmentFeatures,ApartmentCategories
                        cmd.CommandText = "SELECT Apartment.ApartmentID AS ApartmentID,CountryID,Apartment.CategoryID as CategoryID,[Address]" +
                            ",PricePerDay,AvailableFromDate,AvailableToDate,[Description],NumberOfGuests,Shower,Bath,WIFI,TV,Cables,Satellite,Pets" +
                            ",NumberOfBedRooms,LivingRoom,BedRoomDescription,LivingRoomDescription,QueenSizeBed,DoubleBed,SingleBed,SofaBed,BedsDescription,ApartmentType," +
                            "ApartmentImages.PrimeImage,ApartmentImages.Image1,ApartmentImages.Image2,ApartmentImages.Image3,ApartmentImages.Image4," +
                            "ApartmentImages.PrimeImageType,ApartmentImages.ImageType1,ApartmentImages.ImageType2,ApartmentImages.ImageType3,ApartmentImages.ImageType4," +
                            "(SELECT AVG(Reviews.Rating) FROM Reviews WHERE Apartment.ApartmentID = Reviews.ApartmentID) AS AvgRating FROM Apartment INNER JOIN ApartmentImages ON Apartment.ApartmentID = ApartmentImages.ApartmentID INNER JOIN ApartmentFeatures ON" +
                            " Apartment.ApartmentID = ApartmentFeatures.ApartmentID INNER JOIN ApartmentCategories ON Apartment.CategoryID = ApartmentCategories.CategoryID " +
                            "WHERE Apartment.RenterUserName = @UserName";
                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            List<Apartment> temp = new List<Apartment>();
                            while (dr.Read())
                            {
                                Apartment apartment = new Apartment()
                                {
                                    ApartmentID = dr.GetInt32(0),
                                    CountryID = dr.GetInt32(1),//can be localy
                                    CategoryID = dr.GetInt32(2),
                                    Address = dr.GetString(3),
                                    PricePerDay = dr.GetDouble(4),
                                    FromDate = new DateTime(dr.GetInt64(5)),
                                    ToDate = new DateTime(dr.GetInt64(6)),
                                    Description = dr.GetString(7),
                                    NumberOfGuests = dr.GetInt32(8),
                                    Shower = dr.GetBoolean(9),
                                    Bath = dr.GetBoolean(10),
                                    WIFI = dr.GetBoolean(11),
                                    TV = dr.GetBoolean(12),
                                    Cables = dr.GetBoolean(13),
                                    Satellite = dr.GetBoolean(14),
                                    Pets = dr.GetBoolean(15),
                                    NumberOfBedRooms = dr.GetInt32(16),
                                    LivingRoom = dr.GetBoolean(17),
                                    BedRoomDescription = dr.GetString(18),
                                    LivingRoomDescription = dr.GetString(19),
                                    QueenSizeBed = dr.GetInt32(20),
                                    DoubleBed = dr.GetInt32(21),
                                    SingleBed = dr.GetInt32(22),
                                    SofaBed = dr.GetInt32(23),
                                    BedsDescription = dr.GetString(24),
                                    ApartmentType = dr.GetString(25),
                                    AvgRate = dr.IsDBNull(36)? 0 : dr.GetInt32(36)
                                };
                                apartment.ApartmentImageByte = new List<byte[]> { dr.IsDBNull(26) ? null : ((byte[])dr.GetValue(26)) };
                                apartment.ApartmentImageByte.Add(dr.IsDBNull(27) ? null : ((byte[])dr.GetValue(27)));
                                apartment.ApartmentImageByte.Add(dr.IsDBNull(28) ? null : ((byte[])dr.GetValue(28)));
                                apartment.ApartmentImageByte.Add(dr.IsDBNull(29) ? null : ((byte[])dr.GetValue(29)));
                                apartment.ApartmentImageByte.Add(dr.IsDBNull(30) ? null : ((byte[])dr.GetValue(30)));
                                apartment.ApartmentImageType = new string[5];
                                apartment.ApartmentImageType[0] = dr.IsDBNull(31) ? null : dr.GetString(31);
                                apartment.ApartmentImageType[1] = dr.IsDBNull(32) ? null : dr.GetString(32);
                                apartment.ApartmentImageType[2] = dr.IsDBNull(33) ? null : dr.GetString(33);
                                apartment.ApartmentImageType[3] = dr.IsDBNull(34) ? null : dr.GetString(34);
                                apartment.ApartmentImageType[4] = dr.IsDBNull(35) ? null : dr.GetString(35);
                                apartment.ApartmentImage = ImageValidation.BytesToBase64(null, apartment.ApartmentImageByte);
                                apartment.ApartmentImageByte = null;
                                temp.Add(apartment);//adding the apartment to temporary list
                            }
                            userDetails.RenterApartments = temp;

                        }
                        userDetails.PendingOrders = OrdersDB.GetPendingOrders(userName, conn);//check if the user have any pending orders
                        userDetails.Password = null;
                        return userDetails;
                    }

                }
                return null;


            }


        }

        public static class RenterDB
        {

            public static bool BecomeRenter(string userName, SqlConnection conn)// changing your Role to be renter(== 1)
            {

                using (SqlCommand cmd = new SqlCommand("UPDATE Users SET Role=@Role WHERE UserName=@RenterUserName", conn))
                {
                    cmd.Add("@RenterUserName", userName);
                    cmd.Add("@Role", (int)Role.Renter);
                    return cmd.ExecuteNonQuery() == 1;
                }

            }

            public static bool DeleteRenterStatus(string userName)//Deleting the renter status and any apartment the account currently have
            {
                using (SqlConnection conn = new SqlConnection(CONN_STRING))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("UPDATE Users SET Role=@Role WHERE UserName=@RenterUserName", conn))
                    {
                        cmd.Add("@RenterUserName", userName);
                        cmd.Add("@Role", (int)Role.User);
                        if (cmd.ExecuteNonQuery() == 1)
                        {
                            cmd.CommandText = "DELETE FROM Apartment WHERE RenterUserName=@RenterUserName";
                            return cmd.ExecuteNonQuery() >= 1;

                        }
                    }

                    return false;
                }
            }

            public static bool VerifyRenter(string userName)
            {
                using(SqlConnection conn = new SqlConnection(CONN_STRING))
                {
                    conn.Open();
                    using(SqlCommand cmd = new SqlCommand("SELECT Role FROM Users WHERE UserName=@UserName AND Role=1", conn))
                    {
                        cmd.Add("@UserName", userName);
                        using(SqlDataReader dr = cmd.ExecuteReader())
                        {
                            if (dr.HasRows)
                                return true;
                            return false;
                        }
                    }
                }
            }




        }

        public static class ApartmentDB
        {
            public static List<Apartment> GetApartmentsForLocation(int countryID, int numberOfGuests, DateTime fromDate, DateTime toDate) //featching the user search data view Outside Of Page (After the search before choosing the apartment)
            {
                using (SqlConnection conn = new SqlConnection(CONN_STRING))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT Apartment.ApartmentID,RenterUserName,NumberOfBedRooms,QueenSizeBed,DoubleBed,SingleBed,SofaBed,Apartment.CategoryID AS CategoryID,ApartmentType,[Address],PricePerDay,AvailableFromDate,AvailableToDate,Apartment.[Description],ApartmentImages.PrimeImage AS PrimeImage,ApartmentImages.PrimeImageType AS PrimeImageType,(SELECT AVG(Reviews.Rating) FROM Reviews WHERE Apartment.ApartmentID = Reviews.ApartmentID) AS AvgRating FROM Apartment INNER JOIN ApartmentImages ON Apartment.ApartmentID = ApartmentImages.ApartmentID INNER JOIN ApartmentFeatures ON Apartment.ApartmentID=ApartmentFeatures.ApartmentID INNER JOIN ApartmentCategories ON Apartment.CategoryID=ApartmentCategories.CategoryID WHERE Apartment.ApartmentID NOT IN (SELECT Orders.ApartmentID FROM Orders WHERE (Orders.FromDate > @AvailableFromDate OR Orders.ToDate <@AvailableToDate)AND Orders.Approved=1) AND CountryID=@CountryID AND NumberOfGuests >= @NumberOfGuests AND AvailableFromDate<= @AvailableFromDate AND AvailableToDate >= @AvailableToDate", conn))
                    {
                        List<Apartment> apartments = new List<Apartment>();
                        cmd.Add("@CountryID", countryID);
                        cmd.Add("@NumberOfGuests", numberOfGuests);
                        cmd.Add("@AvailableFromDate", fromDate.Ticks);
                        cmd.Add("@AvailableToDate", toDate.Ticks);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Apartment apartment = new Apartment()
                                {
                                    ApartmentID = reader.GetInt32(0),
                                    RenterUserName = reader.GetString(1),
                                    NumberOfBedRooms = reader.GetInt32(2),
                                    QueenSizeBed = reader.GetInt32(3),
                                    DoubleBed = reader.GetInt32(4),
                                    SingleBed = reader.GetInt32(5),
                                    SofaBed = reader.GetInt32(6),
                                    CategoryID = reader.GetInt32(7),
                                    ApartmentType = reader.GetString(8),
                                    Address = reader.GetString(9),
                                    PricePerDay = reader.GetDouble(10),
                                    FromDate = fromDate.Date,
                                    ToDate = toDate.Date,
                                    Description = reader.GetString(13),
                                    PrimeImage = ((byte[])reader.GetValue(14)),
                                    PrimeImageType = reader.GetString(15),
                                    AvgRate = reader.IsDBNull(16) ? 0 : reader.GetInt32(16)
                                };
                                apartment.ApartmentImage = new string[1];
                                apartment.ApartmentImageType = new string[1];
                                apartment.ApartmentImage = ImageValidation.BytesToBase64(apartment.PrimeImage, null);
                                apartment.ApartmentImageType[0] = apartment.PrimeImageType;

                                apartment.NumberOfGuests = numberOfGuests;
                                apartment.CountryID = countryID;
                                apartment.PriceForStaying = (toDate - fromDate).TotalDays > 0 ? (toDate - fromDate).TotalDays * apartment.PricePerDay : apartment.PricePerDay;//caculate the price for staying
                                apartment.TotalNumberOfDays = (toDate - fromDate).TotalDays > 0 ? (toDate - fromDate).TotalDays : 1;//caculate total number of days for the user stay
                                apartment.PricePerGuest = apartment.PriceForStaying / apartment.TotalNumberOfDays / numberOfGuests;//price per guests
                                apartments.Add(apartment);//adding the apartment for the list
                            }
                            return apartments;

                        }
                    }
                }
            }

            public static Apartment AddApartment(Apartment apartment, string userName, bool changeRenterStatus)//adding new apartment (the boolean here is to check if the user does need to change his status)
            {
                using (SqlConnection conn = new SqlConnection(CONN_STRING))
                {
                    conn.Open();
                    if (changeRenterStatus)
                    {
                    if (!RenterDB.BecomeRenter(userName, conn))//if somthing has failed in the role change,terminating request
                        return null;
                    }

                    using (SqlCommand cmd = new SqlCommand("INSERT INTO Apartment(RenterUserName,CountryID,CategoryID,[Address],PricePerDay,AvailableFromDate,AvailableToDate,[Apartment].Description)output INSERTED.ApartmentID VALUES(@RenterUserName,@CountryID,@CategoryID,@Address,@PricePerDay,@AvailableFromDate,@AvailableToDate,@Description)", conn))
                    {
                        cmd.Add("@RenterUserName", userName);
                        cmd.Add("@CountryID", apartment.CountryID);
                        cmd.Add("@CategoryID", apartment.CategoryID);
                        cmd.Add("@Address", apartment.Address);
                        cmd.Add("@PricePerDay", apartment.PricePerDay);
                        cmd.Add("@AvailableFromDate", apartment.FromDate.Ticks);
                        cmd.Add("@AvailableToDate", apartment.ToDate.Ticks);
                        cmd.Add("@Description", apartment.Description);
                        apartment.ApartmentID = (int)cmd.ExecuteScalar();
                        if (AddApartmentFeature(apartment, conn))//if apartmentFeature has add succesfully continute else the proccess is terminate and if the user tried to became a renter his role change back to user
                        {

                            StringBuilder inserter = new StringBuilder();
                            StringBuilder values = new StringBuilder();
                            inserter.Append("INSERT INTO ApartmentImages(ApartmentID,PrimeImage,PrimeImageType");
                            values.Append(" VALUES (@ApartmentID,@PrimeImage,@PrimeImageType");
                            byte[] temp = apartment.ApartmentImageByte[0];
                            cmd.Add("@PrimeImage", temp);
                            cmd.Add("@PrimeImageType", apartment.ApartmentImageType[0]);
                            for (int i = 1; i < apartment.ApartmentImageByte.Count; i++)
                            {
                                temp = apartment.ApartmentImageByte[i];
                                if (apartment.ApartmentImageType[i] != null && temp != null)
                                {
                                    inserter.Append(",Image" + i + ",ImageType" + i);
                                    values.Append(",@Image" + i + ",@ImageType" + i);

                                    cmd.Add("@Image" + i, temp);
                                    cmd.Add("@ImageType" + i, apartment.ApartmentImageType[i]);
                                }

                            }
                            inserter.Append(")");
                            values.Append(")");
                            cmd.Add("@ApartmentID", apartment.ApartmentID);
                            cmd.CommandText = inserter.ToString() + values.ToString();

                            if (cmd.ExecuteNonQuery() == 1)//if image as add success fully continue to add apatment else procces terminated
                            {
                                Apartment tempApartment = new Apartment()
                                {
                                    ApartmentID = apartment.ApartmentID,
                                    FromDate = new DateTime(apartment.FromDate.Ticks),
                                    ToDate = new DateTime(apartment.ToDate.Ticks)
                                };
                              
                                return tempApartment;
                            }

                        }
                        else
                        {
                            DeleteApartment(apartment.ApartmentID, userName, conn);
                            if (changeRenterStatus)
                                RenterDB.DeleteRenterStatus(userName);
                            return null;
                        }
                        return null;
                    }
                }
            }

            public static bool AddApartmentFeature(Apartment features, SqlConnection conn)//adding the apartment features
            {
                using (SqlCommand cmd = new SqlCommand("INSERT INTO ApartmentFeatures(ApartmentID,NumberOfGuests,Shower,Bath,WIFI,TV,Cables,Satellite,Pets,NumberOfBedRooms,LivingRoom,BedRoomDescription,LivingRoomDescription,QueenSizeBed,DoubleBed,SingleBed,SofaBed,BedsDescription)VALUES(@ApartmentID,@NumberOfGuests,@Shower,@Bath,@WIFI,@TV,@Cables,@Satellite,@Pets,@NumberOfBedRooms,@LivingRoom,@BedRoomDescription,@LivingRoomDescription,@QueenSizeBed,@DoubleBed,@SingleBed,@SofaBed,@BedsDescription)", conn))
                {
                    cmd.Add("@ApartmentID", features.ApartmentID);
                    cmd.Add("@NumberOfGuests", features.NumberOfGuests);
                    cmd.Add("@Shower", features.Shower);
                    cmd.Add("@Bath", features.Bath);
                    cmd.Add("@WIFI", features.WIFI);
                    cmd.Add("@TV", features.TV);
                    cmd.Add("@Cables", features.Cables);
                    cmd.Add("@Satellite", features.Satellite);
                    cmd.Add("@Pets", features.Pets);
                    cmd.Add("@NumberOfBedRooms", features.NumberOfBedRooms);
                    cmd.Add("@LivingRoom", features.LivingRoom);
                    cmd.Add("@BedRoomDescription", features.BedRoomDescription);
                    cmd.Add("@LivingRoomDescription", features.LivingRoomDescription);
                    cmd.Add("@QueenSizeBed", features.QueenSizeBed);
                    cmd.Add("@DoubleBed", features.DoubleBed);
                    cmd.Add("@SingleBed", features.SingleBed);
                    cmd.Add("@SofaBed", features.SofaBed);
                    cmd.Add("@BedsDescription", features.BedsDescription);
                    return cmd.ExecuteNonQuery() == 1;
                }
            }

            public static bool DeleteApartment(int apartmentID, string userName, SqlConnection conn)//delete apartment
            {

                if (conn == null)//if the user choose to delete this apartment a new instance of the connection is required
                {
                    conn = new SqlConnection(CONN_STRING);
                }
                using (conn)
                {
                    if (conn.State == System.Data.ConnectionState.Closed)//checking current connection state
                    {
                        conn.Open();

                    }
                    using (SqlCommand cmd = new SqlCommand("DELETE FROM Apartment WHERE ApartmentID=@ApartmentID", conn))
                    {
                        cmd.Add("@ApartmentID", apartmentID);
                        return cmd.ExecuteNonQuery() == 1;
                    }
                }
            }

            public static bool EditApartment(Apartment apartment, bool editFeature, string userName)//edit apartment (features editing is optional) without image
            {
                using (SqlConnection conn = new SqlConnection(CONN_STRING))
                {
                    conn.Open();

                    using (SqlCommand cmd = new SqlCommand("UPDATE Apartment SET CountryID=@CountryID,CategoryID=@CategoryID,[Address]=@Address,PricePerDay=@PricePerDay,AvailableFromDate=@AvailableFromDate,AvailableToDate=@AvailableToDate,Description=@Description WHERE ApartmentID=@ApartmentID AND RenterUserName=@UserName", conn))
                    {
                        cmd.Add("@UserName", userName);
                        cmd.Add("@CountryID", apartment.CountryID);
                        cmd.Add("@CategoryID", apartment.CategoryID);
                        cmd.Add("@Address", apartment.Address);
                        cmd.Add("@PricePerDay", apartment.PricePerDay);
                        cmd.Add("@AvailableFromDate", apartment.FromDate.Ticks);
                        cmd.Add("@AvailableToDate", apartment.ToDate.Ticks);
                        cmd.Add("@Description", apartment.Description);
                        cmd.Add("@ApartmentID", apartment.ApartmentID);
                        if (editFeature)//if edit feature is wanted
                        {
                            cmd.CommandText = "Update ApartmentFeatures SET NumberOfGuests=@NumberOfGuests,Shower=@Shower,Bath=@Bath,WIFI=@WIFI,TV=@TV,Cables=@Cables,Satellite=@Satellite,Pets=@Pets,NumberOfBedRooms=@NumberOfBedRooms,LivingRoom=@LivingRoom,BedRoomDescription=@BedRoomDescription,LivingRoomDescription=@LivingRoomDescription,QueenSizeBed=@QueenSizeBed,DoubleBed=@DoubleBed,SingleBed=@SingleBed,SofaBed=@SofaBed,BedsDescription=@BedsDescription WHERE ApartmentID=@ApartmentID";
                            cmd.Add("@NumberOfGuests", apartment.NumberOfGuests);
                            cmd.Add("@Shower", apartment.Shower);
                            cmd.Add("@Bath", apartment.Bath);
                            cmd.Add("@WIFI", apartment.WIFI);
                            cmd.Add("@TV", apartment.TV);
                            cmd.Add("@Cables", apartment.Cables);
                            cmd.Add("@Satellite", apartment.Satellite);
                            cmd.Add("@Pets", apartment.Pets);
                            cmd.Add("@NumberOfBedRooms", apartment.NumberOfBedRooms);
                            cmd.Add("@LivingRoom", apartment.LivingRoom);
                            cmd.Add("@BedRoomDescription", apartment.BedRoomDescription);
                            cmd.Add("@LivingRoomDescription", apartment.LivingRoomDescription);
                            cmd.Add("@QueenSizeBed", apartment.QueenSizeBed);
                            cmd.Add("@DoubleBed", apartment.DoubleBed);
                            cmd.Add("@SingleBed", apartment.SingleBed);
                            cmd.Add("@SofaBed", apartment.SofaBed);
                            cmd.Add("@BedsDescription", apartment.BedsDescription);
                            return cmd.ExecuteNonQuery() == 1;
                        }
                        return cmd.ExecuteNonQuery() == 1;


                    }
                }
            }

            public static Apartment GetApartment(int apartmentID)//get all of the apartment data after user as choose to view it
            {
                using (SqlConnection conn = new SqlConnection(CONN_STRING))
                {
                    conn.Open();
                    Apartment apartment = new Apartment();
                    using (SqlCommand cmd = new SqlCommand("SELECT RenterUserName,Apartment.CountryID,Apartment.CategoryID,Apartment.[Address],PricePerDay,AvailableFromDate,AvailableToDate,[Description],FirstName,LastName,NumberOfGuests,Shower,Bath,WIFI,TV,Cables,Satellite,Pets,NumberOfBedRooms,LivingRoom,BedRoomDescription,LivingRoomDescription,QueenSizeBed,DoubleBed,SingleBed,SofaBed,BedsDescription,ApartmentType" +
                        ",ApartmentImages.PrimeImage,ApartmentImages.Image1,ApartmentImages.Image2,ApartmentImages.Image3,ApartmentImages.Image4,ApartmentImages.PrimeImageType,ApartmentImages.ImageType1,ApartmentImages.ImageType2,ApartmentImages.ImageType3,ApartmentImages.ImageType4 FROM Apartment INNER JOIN ApartmentImages ON Apartment.ApartmentID = ApartmentImages.ApartmentID INNER JOIN Users ON Apartment.RenterUserName = Users.UserName INNER JOIN ApartmentFeatures ON Apartment.ApartmentID = ApartmentFeatures.ApartmentID INNER JOIN ApartmentCategories ON Apartment.CategoryID = ApartmentCategories.CategoryID WHERE Apartment.ApartmentID = @ApartmentID", conn))
                    {
                        cmd.Add("@ApartmentID", apartmentID);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                apartment.ApartmentImageByte = new List<byte[]>();
                                apartment.ApartmentImageType = new string[5];
                                apartment.ApartmentID = apartmentID;
                                apartment.RenterUserName = reader.GetString(0);
                                apartment.CountryID = reader.GetInt32(1);//can be localy
                                apartment.CategoryID = reader.GetInt32(2);
                                apartment.Address = reader.GetString(3);
                                apartment.PricePerDay = reader.GetDouble(4);
                                apartment.FromDate = new DateTime(reader.GetInt64(5));
                                apartment.ToDate = new DateTime(reader.GetInt64(6));
                                apartment.Description = reader.GetString(7);
                                apartment.FirstName = reader.GetString(8);
                                apartment.LastName = reader.GetString(9);
                                apartment.NumberOfGuests = reader.GetInt32(10);
                                apartment.Shower = reader.GetBoolean(11);
                                apartment.Bath = reader.GetBoolean(12);
                                apartment.WIFI = reader.GetBoolean(13);
                                apartment.TV = reader.GetBoolean(14);
                                apartment.Cables = reader.GetBoolean(15);
                                apartment.Satellite = reader.GetBoolean(16);
                                apartment.Pets = reader.GetBoolean(17);
                                apartment.NumberOfBedRooms = reader.GetInt32(18);
                                apartment.LivingRoom = reader.GetBoolean(19);
                                apartment.BedRoomDescription = reader.GetString(20);
                                apartment.LivingRoomDescription = reader.GetString(21);
                                apartment.QueenSizeBed = reader.GetInt32(22);
                                apartment.DoubleBed = reader.GetInt32(23);
                                apartment.SingleBed = reader.GetInt32(24);
                                apartment.SofaBed = reader.GetInt32(25);
                                apartment.BedsDescription = reader.GetString(26);
                                apartment.ApartmentType = reader.GetString(27);
                                apartment.ApartmentImageByte.Add((byte[])reader.GetValue(28));
                                apartment.ApartmentImageByte.Add(reader.IsDBNull(29) ? null : (byte[])reader.GetValue(29));
                                apartment.ApartmentImageByte.Add(reader.IsDBNull(30) ? null : (byte[])reader.GetValue(30));
                                apartment.ApartmentImageByte.Add(reader.IsDBNull(31) ? null : (byte[])reader.GetValue(31));
                                apartment.ApartmentImageByte.Add(reader.IsDBNull(32) ? null : (byte[])reader.GetValue(32));
                                apartment.ApartmentImageType[0] = reader.IsDBNull(33) ? null : reader.GetString(33);
                                apartment.ApartmentImageType[1] = reader.IsDBNull(34) ? null : reader.GetString(34);
                                apartment.ApartmentImageType[2] = reader.IsDBNull(35) ? null : reader.GetString(35);
                                apartment.ApartmentImageType[3] = reader.IsDBNull(36) ? null : reader.GetString(36);
                                apartment.ApartmentImageType[4] = reader.IsDBNull(37) ? null : reader.GetString(37);
                            }

                        }
                        cmd.CommandText = "SELECT Rating,Description,UserName,ReviewID FROM Reviews WHERE ApartmentID = @ApartmentID";//check if the apartment have reviews
                        try
                        {
                            using (SqlDataReader reader = cmd.ExecuteReader())
                            {
                                List<Reviews> temp = new List<Reviews>();
                                while (reader.Read())
                                {
                                    Reviews reviews = new Reviews()
                                    {
                                        Rating = reader.GetInt16(0),
                                        Description = reader.GetString(1),
                                        UserName = reader.GetString(2),
                                        ReviewID = reader.GetInt32(3)
                                    };
                                    reviews.ApartmentID = apartmentID;
                                    temp.Add(reviews);

                                }
                                apartment.Reviews = temp;
                            }
                        }
                        catch
                        {

                            apartment.Reviews = null;
                        }
                        return apartment;


                    }
                }
            }

            public static bool UpdateApartmentPictures(Apartment apartment)//update the picture for the apartment
            {
                using (SqlConnection conn = new SqlConnection(CONN_STRING))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand())
                    {
                        cmd.Connection = conn;
                        StringBuilder stringBuilder = new StringBuilder();
                        stringBuilder.Append("UPDATE ApartmentImages SET PrimeImage=@PrimeImage");
                        byte[] temp = apartment.ApartmentImageByte[0];
                        cmd.Add("@PrimeImage", temp);
                        cmd.Add("@PrimeImageType", apartment.ApartmentImageType[0]);
                        for (int i = 1; i < apartment.ApartmentImageByte.Count; i++)
                        {
                            temp = apartment.ApartmentImageByte[i];
                            if (apartment.ApartmentImageType[i] != null && temp != null)
                            {
                                stringBuilder.Append(",Image" + i + "=@Image" + i);
                                stringBuilder.Append(",ImageType" + i + "=@ImageType" + i);
                                cmd.Add("@Image" + i, temp);
                                cmd.Add("@ImageType" + i, apartment.ApartmentImageType[i]);
                            }

                        }
                        stringBuilder.Append(" WHERE ApartmentImages.ApartmentID = (SELECT Apartment.ApartmentID FROM Apartment WHERE Apartment.RenterUserName = @RenterUserName AND Apartment.ApartmentID = @ApartmentID)");
                        cmd.Add("@RenterUserName", apartment.RenterUserName);
                        cmd.Add("@ApartmentID", apartment.ApartmentID);
                        cmd.CommandText = stringBuilder.ToString();
                        return cmd.ExecuteNonQuery() == 1;
                    }
                }
            }

            public static List<ApartmentCategories> GetCategories()//get apartments categories
            {
                using (SqlConnection conn = new SqlConnection(CONN_STRING))
                {
                    List<ApartmentCategories> categories = new List<ApartmentCategories>();
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT CategoryID,ApartmentType FROM ApartmentCategories", conn))
                    {
                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {

                            while (dr.Read())
                            {
                                ApartmentCategories category = new ApartmentCategories()
                                {
                                    CategoryID = dr.GetInt32(0),
                                    ApartmentType = dr.GetString(1)
                                };
                                categories.Add(category);

                            }

                            return categories;
                        }
                    }
                }
            }
        }

        public static class CountriesDB
        {
            public static List<Countries> GetCountries()//Featching the countries from db
            {
                List<Countries> countries = new List<Countries>();
                using (SqlConnection conn = new SqlConnection(CONN_STRING))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT CountryID,CountryName FROM Countries", conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Countries country = new Countries()
                                {
                                    CountryID = reader.GetInt32(0),
                                    CountryName = reader.GetString(1)
                                };
                                countries.Add(country);
                            }
                            return countries;
                        }
                    }
                }
            }
        }

        public static class ReviewsDB
        {

            public static int NewReview(Reviews reviews)
            {
                using (SqlConnection conn = new SqlConnection(CONN_STRING)) //adding new review where the user is not the owner
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("IF NOT EXISTS (SELECT Apartment.RenterUserName FROM Apartment WHERE Apartment.ApartmentID = @ApartmentID AND Apartment.RenterUserName = @UserName) INSERT INTO Reviews(Rating,[Description],UserName,ApartmentID) output INSERTED.ReviewID VALUES (@Rating,@Description,@UserName,@ApartmentID)", conn))
                    {
                        try
                        {
                            cmd.Add("@Rating", reviews.Rating);
                            cmd.Add("@UserName", reviews.UserName);
                            cmd.Add("@Description", reviews.Description);
                            cmd.Add("@ApartmentID", reviews.ApartmentID);
                            return reviews.ReviewID = (int)cmd.ExecuteScalar();
                        }
                        catch
                        {
                            return -1;
                        }
                    };
                }
            }

            public static bool DeleteReview(Reviews reviews)//delete the review
            {
                using (SqlConnection conn = new SqlConnection(CONN_STRING))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("DELETE FROM Reviews WHERE ReviewID=@ReviewID AND UserName=@UserName AND ApartmentID=@ApartmentID", conn))
                    {
                        cmd.Add("@ReviewID", reviews.ReviewID);
                        cmd.Add("@ApartmentID", reviews.ApartmentID);
                        cmd.Add("@UserName", reviews.UserName);
                        return cmd.ExecuteNonQuery() == 1;
                    }
                }
            }

            public static bool EditReview(Reviews reviews)//edit review
            {
                using (SqlConnection conn = new SqlConnection(CONN_STRING))
                {
                    conn.Open();

                    using (SqlCommand cmd = new SqlCommand("UPDATE Reviews SET Rating=@Rating,[Description]=@Description WHERE ReviewID=@ReviewID AND UserName=@UserName AND ApartmentID=@ApartmentID", conn))
                    {
                        cmd.Add("@Rating", reviews.Rating);
                        cmd.Add("@Description", reviews.Description);
                        cmd.Add("@ReviewID", reviews.ReviewID);
                        cmd.Add("@ApartmentID", reviews.ApartmentID);
                        cmd.Add("@UserName", reviews.UserName);
                        return cmd.ExecuteNonQuery() == 1;
                    }
                }
            }

            public static List<Reviews> GetUserReviews(string userName)//get all of your reviews
            {
                using (SqlConnection conn = new SqlConnection(CONN_STRING))
                {
                    conn.Open();

                    using (SqlCommand cmd = new SqlCommand("SELECT ReviewID,ApartmentID,Rating,[Description] FROM Reviews WHERE UserName=@UserName", conn))
                    {
                        List<Reviews> reviews = new List<Reviews>();
                        cmd.Add("@UserName", userName);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Reviews review = new Reviews()
                                {
                                    ReviewID = reader.GetInt32(0),
                                    ApartmentID = reader.GetInt32(1),
                                    Rating = reader.GetInt16(2),
                                    Description = reader.GetString(3)
                                };
                                reviews.Add(review);
                            }
                            return reviews;
                        }
                    }
                }
            }



        }

        public static class OrdersDB
        {
            public static bool NewOrder(Orders order)
            {
                using (SqlConnection conn = new SqlConnection(CONN_STRING))
                {
                    conn.Open();
                    double totalDays = (order.ToDate - order.FromDate).TotalDays > 0 ? (order.ToDate - order.FromDate).TotalDays : 1; // caculate total days of staying
                    using (SqlCommand cmd = new SqlCommand("IF NOT EXISTS(SELECT Orders.OrderID FROM Orders WHERE ((Orders.FromDate BETWEEN @FromDate AND @ToDate)" + // check if the user has alredy order apartment in the same dates
                        " OR (Orders.ToDate BETWEEN @FromDate AND @ToDate))" +
                        " AND Orders.UserName = @UserName)" +
                        " INSERT INTO Orders(ApartmentID,RenterUserName,UserName,Price,OrderDate,FromDate,ToDate)" +
                        "VALUES(@ApartmentID,(SELECT Apartment.RenterUserName FROM Apartment WHERE ApartmentID = @ApartmentID)" + //selecting the renter user name from his apartment data
                        ",@UserName,(SELECT Apartment.PricePerDay * @TotalDays FROM Apartment WHERE Apartment.ApartmentID = @ApartmentID)" + // caculate the total price for staying
                        ",@OrderDate,@FromDate,@ToDate )", conn))
                    {
                        cmd.Add("@UserName", order.UserName);
                        cmd.Add("@RenterUserName", order.RenterUserName);
                        cmd.Add("@ApartmentID", order.ApartmentID);
                        cmd.Add("@TotalDays", totalDays);
                        cmd.Add("@OrderDate", DateTime.Now.Ticks);
                        cmd.Add("@FromDate", order.FromDate.Ticks);
                        cmd.Add("@ToDate", order.ToDate.Ticks);
                        return cmd.ExecuteNonQuery() == 1;
                    }
                }
            }

            public static List<Orders> GetUserOrders(string userName)//get all the user past orders
            {
                using (SqlConnection conn = new SqlConnection(CONN_STRING))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT ApartmentID,RenterUserName,Price,OrderDate,FromDate,ToDate,Approved,OrderID FROM Orders WHERE UserName=@UserName", conn))
                    {
                        cmd.Add("@UserName", userName);
                        List<Orders> orders = new List<Orders>();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Orders order = new Orders()
                                {
                                    ApartmentID = reader.GetInt32(0),
                                    RenterUserName = reader.GetString(1),
                                    Price = reader.GetDouble(2),
                                    OrderDate = new DateTime(reader.GetInt64(3)),
                                    FromDate = new DateTime(reader.GetInt64(4)),
                                    ToDate = new DateTime(reader.GetInt64(5)),
                                    Approved = reader.IsDBNull(6) ? null : (bool?)reader.GetBoolean(6),
                                    OrderID = reader.GetInt32(7)

                                };

                                orders.Add(order);
                            }
                            return orders;
                        }
                    }
                }
            }

            public static List<Orders> GetPendingOrders(string userName, SqlConnection conn)//get pending orders
            {
                if (conn == null)
                {
                    conn = new SqlConnection(CONN_STRING);
                }
                using (conn)
                {
                    if (conn.State == System.Data.ConnectionState.Closed)
                    {
                        conn.Open();

                    }
                    using (SqlCommand cmd = new SqlCommand("SELECT ApartmentID,UserName,Price,OrderDate,FromDate,ToDate,OrderID FROM Orders WHERE RenterUserName=@RenterUserName AND Approved IS NULL", conn))
                    {
                        cmd.Add("@RenterUserName", userName);
                        List<Orders> orders = new List<Orders>();
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Orders order = new Orders()
                                {
                                    ApartmentID = reader.GetInt32(0),
                                    UserName = reader.GetString(1),
                                    Price = reader.GetDouble(2),
                                    OrderDate = new DateTime(reader.GetInt64(3)),
                                    FromDate = new DateTime(reader.GetInt64(4)),
                                    ToDate = new DateTime(reader.GetInt64(5)),
                                    OrderID = reader.GetInt32(6)
                                };

                                orders.Add(order);
                            }
                            return orders;
                        }
                    }
                }
            }

            public static List<Orders> GetApartmentOrders(string userName, int apartmentID)//get orders for the current apartment
            {
                using (SqlConnection conn = new SqlConnection(CONN_STRING))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT OrderID,Price,FromDate,ToDate,OrderDate,Approved,UserName FROM Orders WHERE ApartmentID = @ApartmentID AND Approved = 1", conn))
                    {
                        cmd.Add("@ApartmentID", apartmentID);
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            List<Orders> orders = new List<Orders>();
                            while (reader.Read())
                            {
                                Orders order = new Orders()
                                {
                                    ApartmentID = apartmentID,
                                    OrderID = reader.GetInt32(0),
                                    Price = reader.GetDouble(1),
                                    FromDate = new DateTime(reader.GetInt64(2)),
                                    ToDate = new DateTime(reader.GetInt64(3)),
                                    OrderDate = new DateTime(reader.GetInt64(4)),
                                    Approved = reader.GetBoolean(5),
                                    UserName = reader.GetString(6)
                                };
                                orders.Add(order);
                            }
                            return orders;
                        }
                    }
                }
            }

            public static bool DeleteOrder(int orderID, string userName) //delete order
            {
                using(SqlConnection conn = new SqlConnection(CONN_STRING))
                {
                    conn.Open();
                    using(SqlCommand cmd = new SqlCommand("DELETE FROM Orders WHERE OrderID = @OrderID and UserName = @UserName", conn))
                    {
                        cmd.Add("@UserName", userName);
                        cmd.Add("@OrderID", orderID);
                        return cmd.ExecuteNonQuery() == 1;
                    }
                }
            }

            public static object UpdateOrderStatus(Orders orders)
            {
                using (SqlConnection conn = new SqlConnection(CONN_STRING))
                {
                    conn.Open();

                    using (SqlCommand cmd = new SqlCommand())
                    {
                        cmd.Add("@Approved", orders.Approved);
                        cmd.Add("@OrderID", orders.OrderID);
                        cmd.Add("@ApartmentID", orders.ApartmentID);
                        cmd.Connection = conn;
                        if ((bool)orders.Approved) //approved = true
                        {
                            cmd.CommandText = "UPDATE Orders SET Approved = 1 " + //update order status to occupied.. Approved = 1
                                "WHERE Orders.ApartmentID = @ApartmentID AND Orders.OrderID = @OrderID " + 
                                "UPDATE Orders SET Approved = 0 WHERE Orders.OrderID = (SELECT Orders.OrderID FROM Orders " + // set approved to 0 where orders of the same apartment are between the dates of the approved order
                                "JOIN (SELECT Orders.FromDate,Orders.ToDate,OrderID,ApartmentID FROM Orders WHERE Orders.OrderID = @OrderID) AS Step1 ON Step1.ApartmentID = Orders.ApartmentID" + // add the order that as been approved for comparison 
                                " WHERE (Orders.Approved IS NULL OR Orders.Approved = 0) AND Orders.FromDate BETWEEN Step1.FromDate AND Step1.ToDate OR Orders.ToDate BETWEEN Step1.FromDate AND Step1.ToDate AND NOT Orders.OrderID = Step1.OrderID)";
                            if (cmd.ExecuteNonQuery() > 0)
                            {
                                return GetPendingOrders(orders.RenterUserName, conn);
                            }

                        }
                        else if (!(bool)orders.Approved) // approved = false, decline the order
                        {
                            cmd.CommandText = "UPDATE Orders SET Approved = @Approved WHERE Orders.ApartmentID = @ApartmentID AND Orders.OrderID = @OrderID";
                            return cmd.ExecuteNonQuery() == 1;

                        }
                        return false;

                    }

                }
            } //update the order status


        }
    }

   

}

static class SqlCommandExtensions
{
    public static void Add(this SqlCommand cmd, string param, object value)
    {
        cmd.Parameters.AddWithValue(param, value == null ? DBNull.Value : value);
    }
}





