﻿using NTMiner.Core;
using NTMiner.Hub;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace NTMiner {
    public partial class NTMinerRoot : INTMinerRoot {
        #region MineContext
        private class MineContext : IMineContext {
            public event Action OnClose;
            private readonly List<IMessagePathId> _contextHandlers = new List<IMessagePathId>();
            public MineContext(
                string minerName,
                ICoin mainCoin,
                IPool mainCoinPool,
                IKernel kernel,
                IKernelInput kernelInput,
                IKernelOutput kernelOutput,
                ICoinKernel coinKernel,
                string mainCoinWallet,
                string commandLine,
                Dictionary<string, string> parameters,
                Dictionary<Guid, string> fragments,
                Dictionary<Guid, string> fileWriters,
                int[] useDevices) {
                this.Data = new Dictionary<string, object>();
                this.Fragments = fragments;
                this.FileWriters = fileWriters;
                this.Id = Guid.NewGuid();
                this.MinerName = minerName;
                this.MainCoin = mainCoin;
                this.MainCoinPool = mainCoinPool;
                this.Kernel = kernel;
                this.CoinKernel = coinKernel;
                this.MainCoinWallet = mainCoinWallet;
                this.AutoRestartKernelCount = 0;
                this.KernelSelfRestartCount = 0;
                this.CommandLine = commandLine ?? string.Empty;
                this.CreatedOn = DateTime.Now;
                this.Parameters = parameters;
                this.UseDevices = useDevices;
                this.KernelInput = kernelInput;
                this.KernelOutput = kernelOutput;
                string logFileName;
                if (this.CommandLine.Contains(NTKeyword.LogFileParameterName)) {
                    this.KernelProcessType = KernelProcessType.Logfile;
                    logFileName = $"{this.Kernel.Code}_{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss_fff")}.log";
                }
                else {
                    this.KernelProcessType = KernelProcessType.Pip;
                    logFileName = $"{kernel.Code}_pip_{DateTime.Now.Ticks.ToString()}.log";
                }
                this.LogFileFullName = Path.Combine(SpecialPath.LogsDirFullName, logFileName);
            }

            /// <summary>
            /// 命令窗口。使用该方法的代码行应将前两个参数放在第一行以方便vs查找引用时展示出参数信息
            /// </summary>
            public void AddCmdPath<TCmd>(string description, LogEnum logType, Action<TCmd> action, Type location)
                where TCmd : ICmd {
                var messagePathId = VirtualRoot.AddMessagePath(description, logType, action, location);
                _contextHandlers.Add(messagePathId);
            }

            /// <summary>
            /// 事件响应
            /// </summary>
            public void AddEventPath<TEvent>(string description, LogEnum logType, Action<TEvent> action, Type location)
                where TEvent : IEvent {
                var messagePathId = VirtualRoot.AddMessagePath(description, logType, action, location);
                _contextHandlers.Add(messagePathId);
            }

            public void AddOnecePath<TMessage>(string description, LogEnum logType, Action<TMessage> action, Guid pathId, Type location) {
                var messagePathId = VirtualRoot.AddOnecePath(description, logType, action, pathId, location);
                _contextHandlers.Add(messagePathId);
            }

            private bool _isClosed = false;
            public void Close() {
                if (!_isClosed) {
                    _isClosed = true;
                    foreach (var handler in _contextHandlers) {
                        VirtualRoot.DeletePath(handler);
                    }
                    KernelProcess?.Dispose();
                    KernelProcess = null;
                    _contextHandlers.Clear();
                    OnClose?.Invoke();
                }
            }

            public Guid Id { get; private set; }

            public bool IsRestart { get; set; }

            public string MinerName { get; private set; }

            public ICoin MainCoin { get; private set; }

            public IPool MainCoinPool { get; private set; }

            public IKernel Kernel { get; private set; }

            public ICoinKernel CoinKernel { get; private set; }

            public string MainCoinWallet { get; private set; }

            public int AutoRestartKernelCount { get; set; }

            public int KernelSelfRestartCount { get; set; }

            public string LogFileFullName { get; private set; }

            public KernelProcessType KernelProcessType { get; private set; }

            public string CommandLine { get; private set; }

            public DateTime CreatedOn { get; private set; }

            public Dictionary<string, string> Parameters { get; private set; }

            public Dictionary<Guid, string> Fragments { get; private set; }

            public Dictionary<Guid, string> FileWriters { get; private set; }

            public int[] UseDevices { get; private set; }

            public IKernelInput KernelInput { get; private set; }

            public IKernelOutput KernelOutput { get; private set; }

            public Process KernelProcess { get; set; }

            public Dictionary<string, object> Data { get; private set; }
        }
        #endregion

        #region DualMineContext
        private class DualMineContext : MineContext, IDualMineContext {
            public DualMineContext(
                IMineContext mineContext,
                ICoin dualCoin,
                IPool dualCoinPool,
                string dualCoinWallet,
                double dualCoinWeight,
                Dictionary<string, string> parameters,
                Dictionary<Guid, string> fragments,
                Dictionary<Guid, string> fileWriters,
                int[] useDevices) : base(
                    mineContext.MinerName,
                    mineContext.MainCoin,
                    mineContext.MainCoinPool,
                    mineContext.Kernel,
                    mineContext.KernelInput,
                    mineContext.KernelOutput,
                    mineContext.CoinKernel,
                    mineContext.MainCoinWallet,
                    mineContext.CommandLine,
                    parameters,
                    fragments,
                    fileWriters,
                    useDevices) {
                this.DualCoin = dualCoin;
                this.DualCoinPool = dualCoinPool;
                this.DualCoinWallet = dualCoinWallet;
                this.DualCoinWeight = dualCoinWeight;
            }


            public ICoin DualCoin { get; private set; }

            public IPool DualCoinPool { get; private set; }

            public string DualCoinWallet { get; private set; }

            public double DualCoinWeight { get; private set; }
        }
        #endregion
    }
}
