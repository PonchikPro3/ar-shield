// Bake this project's static Sunny Kitty GLB into Unity's native OBJ + texture.
// Geometry, normals and UVs are preserved; the two source root rotations cancel.
const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const source = path.resolve(__dirname, '../assets/sunny_kitty.glb');
const bytes = fs.readFileSync(source);
const size = bytes.readUInt32LE(12);
const gltf = JSON.parse(bytes.subarray(20,20+size).toString());
const bin = bytes.subarray(28+size);
if(gltf.meshes.length!==1 || gltf.meshes[0].primitives.length!==1 || gltf.animations || gltf.skins)
  throw Error('Converter expects the original static Sunny Kitty mesh');
const primitive=gltf.meshes[0].primitives[0];
function accessor(index) {
  const a=gltf.accessors[index],v=gltf.bufferViews[a.bufferView];
  const count={SCALAR:1,VEC2:2,VEC3:3}[a.type];
  if(!count || ![5125,5126].includes(a.componentType)) throw Error('Unexpected accessor type');
  const result=[];
  for(let i=0;i<a.count;i++) {
    const row=[];
    for(let j=0;j<count;j++) {
      const offset=(v.byteOffset||0)+(a.byteOffset||0)+i*(v.byteStride||count*4)+j*4;
      row.push(a.componentType===5126?bin.readFloatLE(offset):bin.readUInt32LE(offset));
    }
    result.push(row);
  }
  return result;
}
const pos=accessor(primitive.attributes.POSITION), normals=accessor(primitive.attributes.NORMAL);
const uv=accessor(primitive.attributes.TEXCOORD_0), indices=accessor(primitive.indices).flat();
const lines=['# Sunny Kitty by Ponter, CC-BY-4.0. See assets/SUNNY-KITTY-LICENSE.md','o SunnyKitty'];
pos.forEach(v=>lines.push('v '+v.join(' ')));
uv.forEach(v=>lines.push(`vt ${v[0]} ${1-v[1]}`));
normals.forEach(v=>lines.push('vn '+v.join(' ')));
for(let i=0;i<indices.length;i+=3) lines.push('f '+indices.slice(i,i+3).map(x=>`${x+1}/${x+1}/${x+1}`).join(' '));
const output=path.resolve(__dirname,'../UnityProject/Assets/Models');fs.mkdirSync(output,{recursive:true});
fs.writeFileSync(path.join(output,'SunnyKitty.obj'),lines.join('\n')+'\n');
const image=gltf.images[0], view=gltf.bufferViews[image.bufferView];
fs.writeFileSync(path.join(output,'KittyBaseColor.png'),bin.subarray(view.byteOffset,view.byteOffset+view.byteLength));
const report={source:'assets/sunny_kitty.glb',sha256:crypto.createHash('sha256').update(bytes).digest('hex'),
  vertices:pos.length,triangles:indices.length/3,simplified:false,textureBytes:view.byteLength,attribution:gltf.asset.extras};
fs.writeFileSync(path.join(output,'conversion.json'),JSON.stringify(report,null,2));console.log(report);
