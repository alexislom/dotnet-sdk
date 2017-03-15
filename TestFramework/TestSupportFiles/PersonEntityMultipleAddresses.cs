using Newtonsoft.Json;
using SQLite.Net.Attributes;
using Kinvey;
using System.Collections.Generic;
namespace TestFramework
{
	[JsonObject(MemberSerialization.OptIn)]
	public class PersonEntityMultipleAddresses : Entity
	{
		[JsonProperty]
		public string FirstName { get; set; }

		[JsonProperty]
		public string LastName { get; set; }

		[JsonProperty]
		public string MailAddressList
		{
			get
			{
				return JsonConvert.SerializeObject(MailAddress);
			}
			set
			{
				MailAddress = JsonConvert.DeserializeObject<List<AddressEntity>>(value);
			}
		}

		[SQLite.Net.Attributes.Ignore]
		public List<AddressEntity> MailAddress { get; set; }

		public PersonEntityMultipleAddresses()
		{
			//MailAddress = new List<AddressEntity>();
		}
	}
}
