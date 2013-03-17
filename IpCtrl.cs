using System;
using System.Management;
using System.Windows.Forms;


public class IpCtrl {
    public ManagementObjectCollection getAdapters () {
        ManagementClass objMC = new ManagementClass("Win32_NetworkAdapterConfiguration");
        return objMC.GetInstances();
    }

    public bool isUsingDhcp (ManagementObject mo) {
        return (bool)mo["DHCPEnabled"];
    }

    public void EnableDHCP (ManagementObject mo) {
        ManagementBaseObject methodParams = mo.GetMethodParameters("EnableDHCP");
        ManagementBaseObject renewParams = mo.GetMethodParameters("RenewDHCPLease");

        mo.InvokeMethod("EnableDHCP", methodParams, null);
        mo.InvokeMethod("RenewDHCPLease", renewParams, null);
    }

    public void EnableAutoDns (ManagementObject mo) {
        //
        setDNS (mo, new string[]{} );
    }

    public void setIP (ManagementObject objMO, string IPAddress, string SubnetMask, string Gateway) {
        try {
            ManagementBaseObject objNewIP, objNewGate, objNewDns;
            objNewIP = objMO.GetMethodParameters("EnableStatic");
            objNewGate = objMO.GetMethodParameters("SetGateways");
            objNewDns = objMO.GetMethodParameters("EnableDNS");

            objNewGate["DefaultIPGateway"] = new string[] { Gateway };
            objNewGate["GatewayCostMetric"] = new int[] { 1 };
            objNewIP["IPAddress"] = new string[] { IPAddress };
            objNewIP["SubnetMask"] = new string[] { SubnetMask };
            //objNewDns["DNSServerSearchOrder"] = new string[] { DNS1, DNS2 };

            objMO.InvokeMethod("SetDNSServerSearchOrder", objNewDns, null);
            objMO.InvokeMethod("EnableStatic", objNewIP, null);
            objMO.InvokeMethod("SetGateways", objNewGate, null);

            //MessageBox.Show("Updated IPAddress, SubnetMask and Default Gateway!");
        }
        catch (Exception ex) {
            MessageBox.Show("Unable to Set IP : " + ex.Message);
        }
    }

    public void ListIP () {
        try {
            ManagementClass objMC = new ManagementClass("Win32_NetworkAdapterConfiguration");
            ManagementObjectCollection objMOC = objMC.GetInstances();

            foreach (ManagementObject objMO in objMOC) {
                if (!(bool)objMO["ipEnabled"])
                    continue;

                MessageBox.Show(objMO["Caption"] + "," + objMO["ServiceName"] + "," + objMO["MACAddress"]);
                string[] ipaddresses = (string[])objMO["IPAddress"];
                string[] subnets = (string[])objMO["IPSubnet"];
                string[] gateways = (string[])objMO["DefaultIPGateway"];

                MessageBox.Show(objMO["DefaultIPGateway"].ToString());

                MessageBox.Show("IPGateway");
                foreach (string sGate in gateways)
                    MessageBox.Show(sGate);


                MessageBox.Show("Ipaddress");
                foreach (string sIP in ipaddresses)
                    MessageBox.Show(sIP);

                MessageBox.Show("SubNet");
                foreach (string sNet in subnets)
                    MessageBox.Show(sNet);

            }
        }
        catch (Exception ex) {
            MessageBox.Show(ex.Message);
        }
    }

    public void setDNS (ManagementObject mo, string[] servers) {
        ManagementBaseObject methodParams = mo.GetMethodParameters("SetDNSServerSearchOrder");
        methodParams["DNSServerSearchOrder"] = servers;
        
        try {
            mo.InvokeMethod("SetDNSServerSearchOrder", methodParams, null);
        }
        catch (Exception e) {
            Console.WriteLine("Failed to set DNS", e);
        }
    }

    public IpCtrl () { }

}
