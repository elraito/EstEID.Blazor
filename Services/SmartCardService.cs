using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PCSC;
using PCSC.Exceptions;
using PCSC.Iso7816;
using PCSC.Monitoring;
using PCSC.Utils;

namespace EstEID.Blazor.Services
{
    public class SmartCardService : IHostedService, IDisposable
    {
        private readonly ILogger<SmartCardService> logger;
        private static readonly IContextFactory SmartCardContextFactory = ContextFactory.Instance;
        private static readonly IMonitorFactory SmartCardMonitorFactory = MonitorFactory.Instance;
        private ISCardContext cardContext = SmartCardContextFactory.Establish(SCardScope.System);
        private ISCardMonitor monitorContext = SmartCardMonitorFactory.Create(SCardScope.System);


        public SmartCardService(ILogger<SmartCardService> logger)
        {
            this.logger = logger;
            /*             ISCardContext cardContext = SmartCardContextFactory.Establish(SCardScope.System);
             */
            /*             ISCardMonitor monitorContext = SmartCardMonitorFactory.Create(SCardScope.System);
             */
            monitorContext.Start(cardContext.GetReaders());

            AttachMonitorEvents(monitorContext);

            foreach (string readerName in cardContext.GetReaders())
            {
                logger.LogInformation($"Found reader {readerName}");
            }
        }

        public void Dispose()
        {
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Starting reader monitor.");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Killing PC/SC context.");
            return Task.CompletedTask;
        }

        private void AttachMonitorEvents(ISCardMonitor monitor)
        {
            monitor.Initialized += OnCardInitialized;
            monitor.CardInserted += OnCardInserted;
            monitor.CardRemoved += OnCardRemoved;
            monitor.MonitorException += OnMonitorException;
        }

        private void OnMonitorException(object sender, PCSCException exception)
        {
            logger.LogWarning($"Monitor encountered issue: {SCardHelper.StringifyError(exception.SCardError)}");
        }

        private void OnCardRemoved(object sender, CardStatusEventArgs e)
        {
            logger.LogInformation($"Card removed from {e.ReaderName}");
        }

        private void OnCardInserted(object sender, CardStatusEventArgs e)
        {
            logger.LogInformation($"Card inserted to: {e.ReaderName}");
            string[] records = new string[16];

            using (var cardContext = SmartCardContextFactory.Establish(SCardScope.System))
            {
                using (IsoReader isoReader = new IsoReader(
                    context: cardContext,
                    readerName: e.ReaderName,
                    mode: SCardShareMode.Shared,
                    protocol: SCardProtocol.Any,
                    releaseContextOnDispose: true
                ))
                {
                    var apdu = new CommandApdu(IsoCase.Case2Short, isoReader.ActiveProtocol) { CLA = 0x00, Instruction = InstructionCode.GetChallenge, P1 = 0x00, P2 = 0x00, Le = 0x08 };
                    var selectMF = new CommandApdu(IsoCase.Case2Short, isoReader.ActiveProtocol) { CLA = 0x00, Instruction = InstructionCode.SelectFile, P1 = 0x00, P2 = 0x0C, Le = 0 };
                    var selectEEEE = new CommandApdu(IsoCase.Case4Short, isoReader.ActiveProtocol) { CLA = 0x00, Instruction = InstructionCode.SelectFile, P1 = 0x01, P2 = 0x0C, Data = new byte[] { 0xEE, 0xEE }, Le = 0x08 };
                    var select5044 = new CommandApdu(IsoCase.Case4Short, isoReader.ActiveProtocol) { CLA = 0x00, Instruction = InstructionCode.SelectFile, P1 = 2, P2 = 0x0C, Data = new byte[] { 0x50, 0x44 }, Le = 2 };
                    var readRecord = new CommandApdu(IsoCase.Case2Short, isoReader.ActiveProtocol) { CLA = 0x00, Instruction = InstructionCode.ReadRecord, P1 = 0, P2 = 4, Le = 0 };
                    isoReader.Transmit(selectMF);
                    isoReader.Transmit(selectEEEE);
                    isoReader.Transmit(select5044);

                    for (byte j = 1; j < 16; j++)
                    {
                        readRecord.P1 = j;
                        var response = isoReader.Transmit(readRecord);
                        if (!response.HasData)
                        {
                            logger.LogWarning("Card does not understand instructions");
                        }
                        else
                        {
                            var data = response.GetData();
                            records[j - 1] = Encoding.UTF8.GetString(data).Trim();
                            System.Console.WriteLine(Encoding.UTF8.GetString(data).Trim());
                        }
                    }
                }
            }
        }

        private void OnCardInitialized(object sender, CardStatusEventArgs e)
        {
            logger.LogInformation($"Initialized: {e.ReaderName}");
        }
    }
}