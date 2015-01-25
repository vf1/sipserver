using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using EnhancedPresence;
using ServiceSoap.XmlContent;

namespace ServiceSoap
{
	class ServiceSoap1
	{
		public ServiceSoap1()
		{
			MaxResults = 100;
		}

		public int MaxResults { get; set; }

		public delegate DirectorySearchResponse SearchDelegate<T>(ServiceSoap1 service, DirectorySearchRequest request, T param);

		public OutContent ProcessRequest<T>(ArraySegment<byte> content, SearchDelegate<T> searchHandler, T param)
		{
			try
			{
				List<DirectorySearchItem> foundItems = null;
				bool moreAvailable = false;

				var request = DirectorySearchRequest.Parse(CreateXmlReader(content));

				int maxResults = Math.Min(MaxResults, request.MaxResults);

				foreach (var searchTerm in request.SearchTerms)
				{
					const string testPrefix = @"TESTJDOE";
					if (searchTerm.Key == @"givenName" && string.Compare(searchTerm.Value, 0, testPrefix, 0, testPrefix.Length) == 0)
					{
						int requestedResults;
						if (int.TryParse(searchTerm.Value.Substring(testPrefix.Length), out requestedResults))
						{
							foundItems = new List<DirectorySearchItem>();

							for (int i = 0; i < maxResults && i < requestedResults; i++)
								foundItems.Add(new DirectorySearchItem()
									{
										Uri = string.Format(@"jdoe{0}@officesip.local", i),
										DisplayName = string.Format(@"Joe Doe {0}", i),
										Title = string.Format(@"Title {0}", i),
										Office = string.Format(@"#{0}", i),
										Phone = string.Format(@"+1 111 111 1111 ext.{0}", i),
										Company = string.Format(@"Company #{0}", i),
										City = string.Format(@"City{0}", i),
										State = string.Format(@"State{0}", i),
										Country = string.Format(@"Country{0}", i),
										Email = string.Format(@"jdoe{0}@officesip.com", i),
									});

							moreAvailable = (maxResults < requestedResults);
						}
						break;
					}
				}

				DirectorySearchResponse response = null;

				if (foundItems == null && searchHandler != null)
				{
					response = searchHandler(this, request, param);
				}
				else
				{
					response = new DirectorySearchResponse()
					{
						Items = foundItems,
						MoreAvailable = moreAvailable,
					};
				}

				return new OutContent(response, new object());
			}
			catch (Exception)
			{
				return new OutContent(
					new DirectorySearchResponse()
					{
						Items = new List<DirectorySearchItem>(),
					},
					new object());
			}
		}

		private XmlReader CreateXmlReader(ArraySegment<byte> content)
		{
			MemoryStream memoryStream = new MemoryStream(content.Array, content.Offset, content.Count);
			StreamReader streamReader = new StreamReader(memoryStream, System.Text.Encoding.UTF8);

			return XmlReader.Create(streamReader);
		}
	}
}
