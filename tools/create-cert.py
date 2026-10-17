"""Create a private development CA and a LAN server certificate (cryptography)."""
import datetime
import ipaddress
import pathlib
import sys
from cryptography import x509
from cryptography.hazmat.primitives import hashes, serialization
from cryptography.hazmat.primitives.asymmetric import rsa
from cryptography.x509.oid import NameOID, ExtendedKeyUsageOID

out = pathlib.Path(__file__).resolve().parent.parent / '.certs'
out.mkdir(exist_ok=True)
now = datetime.datetime.now(datetime.timezone.utc)
ca_key = rsa.generate_private_key(public_exponent=65537, key_size=2048)
ca_name = x509.Name([x509.NameAttribute(NameOID.COMMON_NAME, 'AR Shield Local Development CA')])
ca = (x509.CertificateBuilder().subject_name(ca_name).issuer_name(ca_name)
      .public_key(ca_key.public_key()).serial_number(x509.random_serial_number())
      .not_valid_before(now-datetime.timedelta(minutes=5)).not_valid_after(now+datetime.timedelta(days=365))
      .add_extension(x509.BasicConstraints(ca=True, path_length=0), critical=True)
      .add_extension(x509.KeyUsage(False,False,False,False,False,True,True,False,False), critical=True)
      .sign(ca_key, hashes.SHA256()))
key = rsa.generate_private_key(public_exponent=65537, key_size=2048)
ips = sorted(set(['127.0.0.1', '::1'] + sys.argv[1:]))
names = [x509.DNSName('localhost')] + [x509.IPAddress(ipaddress.ip_address(ip)) for ip in ips]
leaf = (x509.CertificateBuilder().subject_name(x509.Name([x509.NameAttribute(NameOID.COMMON_NAME, 'AR Shield HTTPS')]))
        .issuer_name(ca.subject).public_key(key.public_key()).serial_number(x509.random_serial_number())
        .not_valid_before(now-datetime.timedelta(minutes=5)).not_valid_after(now+datetime.timedelta(days=30))
        .add_extension(x509.SubjectAlternativeName(names), critical=False)
        .add_extension(x509.BasicConstraints(ca=False, path_length=None), critical=True)
        .add_extension(x509.ExtendedKeyUsage([ExtendedKeyUsageOID.SERVER_AUTH]), critical=False)
        .sign(ca_key, hashes.SHA256()))
(out/'server-key.pem').write_bytes(key.private_bytes(serialization.Encoding.PEM, serialization.PrivateFormat.PKCS8, serialization.NoEncryption()))
(out/'server.pem').write_bytes(leaf.public_bytes(serialization.Encoding.PEM)+ca.public_bytes(serialization.Encoding.PEM))
(out/'rootCA.cer').write_bytes(ca.public_bytes(serialization.Encoding.DER))
# The CA private key is intentionally not saved. Regenerate for changed IPs.
print('HTTPS certificate created for: '+', '.join(ips))
