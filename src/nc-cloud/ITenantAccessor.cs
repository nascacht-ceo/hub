using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace nc.Cloud;

public interface ITenantAccessor
{
	//IDisposable SetTenantName(string name);

	//string? GetTenantName();

	IDisposable SetTenant(ITenant tenant);

	ITenant? GetTenant();
}

public interface ITenantAccessor<T>: ITenantAccessor
{ }

public class TenantAccessor: ITenantAccessor
{
	// private readonly AsyncLocal<string?> _tenantName = new();

	private readonly AsyncLocal<ITenant?> _tenant = new();
	//public IDisposable SetTenantName(string name)
	//{
	//	var previousTenant = _tenantName.Value;
	//	_tenantName.Value = name;
	//	return new DisposeAction(() => _tenantName.Value = previousTenant);
	//}

	//public string? GetTenantName()
	//{
	//	return _tenantName.Value;
	//}

	public IDisposable SetTenant(ITenant tenant)
	{
		var previousTenant = _tenant.Value;
		_tenant.Value = tenant;
		return new DisposeAction(() => _tenant.Value = previousTenant);
	}

	public ITenant? GetTenant()
	{
		return _tenant.Value;
	}

	internal sealed class DisposeAction : IDisposable
	{
		private readonly Action _action;
		private bool _disposed;

		public DisposeAction(Action action)
		{
			_action = action ?? throw new ArgumentNullException(nameof(action));
		}

		public void Dispose()
		{
			if (!_disposed)
			{
				_action();
				_disposed = true;
			}
		}
	}
}

public class TenantAccessor<T>: TenantAccessor, ITenantAccessor<T>
{ }