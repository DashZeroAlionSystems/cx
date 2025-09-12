using Microsoft.Extensions.Hosting;

namespace CX.Engine.Common.RegistrationServices;

public delegate Task StartupTaskFunc(IHost host);
