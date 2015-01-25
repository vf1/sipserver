using System;
using System.Collections.Generic;
using System.Threading;
using ControlPanel.Service1;
// http://bea.stollnitz.com/blog/?p=426
// based on Paul McClean's
// http://www.codeproject.com/KB/WPF/WpfDataVirtualization.aspx
using DataVirtualization;

namespace ControlPanel
{
	class UsersFetcher
		: IItemsProvider<Service1.User>
	{
		private string id;
		private IWcfService service;

		public UsersFetcher(IWcfService service, string id)
		{
			this.id = id;
			this.service = service;
		}

		public int FetchCount()
		{
			return service.GetUsersCount(id);
		}

		public IList<User> FetchRange(int startIndex, int pageCount, out int overallCount)
		{
			return service.GetUsers(out overallCount, id, startIndex, pageCount);
		}
	}
}
