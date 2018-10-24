﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Apartrent_Try1
{
    public class Users
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public bool Gender { get; set; }
        public string Address { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public long LastLogin { get; set; }
        public long LastOrder { get; set; }
        public int CountryID { get; set; }
        public string CountryName { get; set; }
        public int Role { get; set; }
        public List<Apartment> RenterApartments { get; set; }

    }

    public enum Role{
        User,Renter,Admin
    }

}
