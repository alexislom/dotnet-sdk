using Kinvey;
using Newtonsoft.Json;
using System;

namespace testiosapp2
{
	class hierarchy : Entity
	{

		[JsonProperty("SalesOrganization")]
		public string SalesOrganization { get; set; }

		[JsonProperty("DistributionChannel")]
		public string DistributionChannel { get; set; }

		[JsonProperty("MaterialNumber")]
		public String MaterialNumber { get; set; }

		[JsonProperty("ConditionType")]
		public string ConditionType { get; set; }

		[JsonProperty("ValidityStartDate")]
		public DateTime? ValidityStartDate { get; set; }

		[JsonProperty("ValidityEndDate")]
		public DateTime? ValidityEndDate { get; set; }

		[JsonProperty("Price")]
		public decimal Price { get; set; }

		[JsonProperty("Currency")]
		public string Currency { get; set; }

		[JsonProperty("DeliveryUnit")]
		public string DeliveryUnit { get; set; }

		[JsonProperty("UnitQuantity")]
		public string UnitQuantity { get; set; }

		[JsonProperty("UnitOfMeasure")]
		public string UnitOfMeasure { get; set; }

		[JsonProperty("SAPCustomerNumber")]
		public string SAPCustomerNumber { get; set; }
	}
}