using System.Linq;
using System.Reflection;
using System.ServiceModel;
using System.Xml.Serialization;

namespace SoapCore
{
	public class OperationDescription
	{
		public OperationDescription(ContractDescription contract, MethodInfo operationMethod, OperationContractAttribute contractAttribute)
		{
			Contract = contract;
			Name = contractAttribute.Name ?? operationMethod.Name;
			SoapAction = contractAttribute.Action ?? $"{contract.Namespace.TrimEnd('/')}/{contract.Name}/{Name}";
			IsOneWay = contractAttribute.IsOneWay;
			IsMessageContractResponse =
				operationMethod
					.ReturnType
					.CustomAttributes
					.FirstOrDefault(ca => ca.AttributeType == typeof(MessageContractAttribute)) != null;
			ReplyAction = contractAttribute.ReplyAction;
			DispatchMethod = operationMethod;

			AllParameters = operationMethod.GetParameters()
				.Select((info, index) => CreateParameterInfo(info, index, contract))
				.ToArray();
			InParameters = AllParameters
				.Where(soapParam => soapParam.Direction != SoapMethodParameterDirection.OutOnlyRef)
				.ToArray();
			OutParameters = AllParameters
				.Where(soapParam => soapParam.Direction != SoapMethodParameterDirection.InOnly)
				.ToArray();

			IsMessageContractRequest =
				InParameters.Length == 1
				&& InParameters.First().Parameter.ParameterType
					.CustomAttributes
					.FirstOrDefault(ca =>
						ca.AttributeType == typeof(MessageContractAttribute)) != null;

			ReturnName = operationMethod.ReturnParameter.GetCustomAttribute<MessageParameterAttribute>()?.Name ?? Name + "Response";
		}

		public ContractDescription Contract { get; private set; }
		public string SoapAction { get; private set; }
		public string ReplyAction { get; private set; }
		public string Name { get; private set; }
		public MethodInfo DispatchMethod { get; private set; }
		public bool IsOneWay { get; private set; }
		public bool IsMessageContractResponse { get; private set; }
		public bool IsMessageContractRequest { get; private set; }
		public SoapMethodParameterInfo[] AllParameters { get; private set; }
		public SoapMethodParameterInfo[] InParameters { get; private set; }
		public SoapMethodParameterInfo[] OutParameters { get; private set; }
		public string ReturnName { get; private set; }

		private static SoapMethodParameterInfo CreateParameterInfo(ParameterInfo info, int index, ContractDescription contract)
		{
			var elementAttribute = info.GetCustomAttribute<XmlElementAttribute>();
			var parameterName =
				elementAttribute?.ElementName ??
				info.GetCustomAttribute<MessageParameterAttribute>()?.Name ??
				info.ParameterType.GetCustomAttribute<MessageContractAttribute>()?.WrapperName ??
				info.Name;
			var parameterNs = elementAttribute?.Namespace ?? contract.Namespace;
			return new SoapMethodParameterInfo(info, index, parameterName, parameterNs);
		}
	}
}
