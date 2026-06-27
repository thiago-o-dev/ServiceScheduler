using SharedKernel.Exceptions;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceScheduler.Application.Exceptions;

public class DuplicateEntityException(string message) : BusinessLogicException(message);
