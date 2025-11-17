using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nc.Cloud;

public interface ITenantAccessor
{
	IDisposable SetTenant(string name);

	string? GetTenant();
}

public interface ITenantAccessor<T>: ITenantAccessor
{ }

public class TenantAccessor: ITenantAccessor
{
	private readonly AsyncLocal<string?> _tenant = new();
	public IDisposable SetTenant(string name)
	{
		var previousTenant = _tenant.Value;
		_tenant.Value = name;
		return new DisposeAction(() => _tenant.Value = previousTenant);
	}

	public string? GetTenant()
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