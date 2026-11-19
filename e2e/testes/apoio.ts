import { createHmac } from 'node:crypto'
import { crc32, deflateSync } from 'node:zlib'

// Código TOTP (RFC 6238: SHA-1, 6 dígitos, 30 s), como a app autenticadora o calcula.
export function codigoTotp(segredoBase32: string, agora = Date.now()): string {
  const alfabeto = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ234567'
  let bits = ''
  for (const c of segredoBase32.replace(/=+$/, '').toUpperCase()) bits += alfabeto.indexOf(c).toString(2).padStart(5, '0')
  const chave = Buffer.from(bits.match(/.{8}/g)!.map((b) => parseInt(b, 2)))

  const contador = Buffer.alloc(8)
  contador.writeBigUInt64BE(BigInt(Math.floor(agora / 1000 / 30)))
  const hmac = createHmac('sha1', chave).update(contador).digest()
  const inicio = hmac[hmac.length - 1]! & 0x0f
  const numero = (hmac.readUInt32BE(inicio) & 0x7fffffff) % 1_000_000
  return numero.toString().padStart(6, '0')
}

// PNG com um gradiente, gerado na hora (sem ficheiros de imagem no repositório).
export function imagemPng(largura = 160, altura = 90): Buffer {
  const linhas = Buffer.alloc((largura * 3 + 1) * altura)
  for (let y = 0; y < altura; y++) {
    const base = y * (largura * 3 + 1)
    for (let x = 0; x < largura; x++) {
      linhas[base + 1 + x * 3] = Math.round((x / largura) * 255)
      linhas[base + 2 + x * 3] = 80
      linhas[base + 3 + x * 3] = Math.round((y / altura) * 255)
    }
  }
  const bloco = (tipo: string, dados: Buffer) => {
    const tamanho = Buffer.alloc(4)
    tamanho.writeUInt32BE(dados.length)
    const verificacao = Buffer.alloc(4)
    verificacao.writeUInt32BE(crc32(Buffer.concat([Buffer.from(tipo), dados])))
    return Buffer.concat([tamanho, Buffer.from(tipo), dados, verificacao])
  }
  const cabecalho = Buffer.alloc(13)
  cabecalho.writeUInt32BE(largura, 0)
  cabecalho.writeUInt32BE(altura, 4)
  cabecalho.set([8, 2, 0, 0, 0], 8)
  return Buffer.concat([
    Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]),
    bloco('IHDR', cabecalho),
    bloco('IDAT', deflateSync(linhas)),
    bloco('IEND', Buffer.alloc(0)),
  ])
}
