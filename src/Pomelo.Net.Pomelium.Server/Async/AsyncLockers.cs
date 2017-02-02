using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pomelo.Net.Pomelium.Server.Async
{
    public class AsyncLockers
    {
        public AsyncSemaphore GroupMembersOperationLocker { get; private set; } = new AsyncSemaphore();
        public AsyncSemaphore ClientJoinedGroupsOperationLocker { get; private set; } = new AsyncSemaphore();
        public AsyncSemaphore ClientsOfNodeOperationLocker { get; private set; } = new AsyncSemaphore();
        public AsyncSemaphore SessionOperationLocker { get; private set; } = new AsyncSemaphore();
        public AsyncSemaphore ClientOwnedSessionKeysOperationLocker { get; private set; } = new AsyncSemaphore();
        public AsyncSemaphore GarbageOperationLocker { get; private set; } = new AsyncSemaphore();
    }
}
