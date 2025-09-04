using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameController.Shared.Models
{
	public class OperationResult
	{
		public Boolean Result { get; set; }

		public String Message { get; set; }

		public Guid? ObjectID { get; set; }

		public Object Results { get; set; }
		public OperationResult()
		{

		}

		public OperationResult(bool result)
		{
			if (result)
				SetSuccess();
		}

		public void SetSuccess()
		{
			Message = "ოპერაცია წარმატებით განხორციელდა";
			Result = true;
		}
		public void SetSuccess(String message)
		{
			Message = message;
			Result = true;
		}

		public void SetError(String message)
		{
			Message = message;
			Result = false;

		}

	}
}
