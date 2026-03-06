#!/bin/bash
echo "Looking for N+1 update patterns..."
grep -A 5 -B 5 "foreach" Rvnx.CRM.Core/Services/ContactManagementService.cs | grep "UpdateAsync"
echo "Done."
