using System;
using Ses.Abstracts;
using Ses.Abstracts.Contracts;
using Ses.Abstracts.Converters;

namespace Ses.Subscriptions
{
    internal class PoolerContext
    {
        public IContractsRegistry ContractsRegistry { get; private set; }
        public ILogger Logger { get; private set; }
        public IPoolerStateRepository StateRepository { get; private set; }
        public IUpConverterFactory UpConverterFactory { get; private set; }

        public PoolerContext(IContractsRegistry contractsRegistry, ILogger logger, IPoolerStateRepository stateRepository, IUpConverterFactory upConverterFactory)
        {
            if (contractsRegistry == null) throw new ArgumentNullException(nameof(contractsRegistry));
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            if (stateRepository == null) throw new ArgumentNullException(nameof(stateRepository));

            ContractsRegistry = contractsRegistry;
            Logger = logger;
            StateRepository = stateRepository;
            UpConverterFactory = upConverterFactory;
        }
    }
}