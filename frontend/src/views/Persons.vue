<template>
  <div class="page-container">
    <div class="page-header">
      <div>
        <h3 style="margin:0">人员管理</h3>
        <span style="color:#909399;font-size:13px">管理司机、安全员、监理人员及资质证书</span>
      </div>
      <div class="header-actions">
        <el-select v-model="roleFilter" placeholder="角色筛选" clearable style="width:140px" size="default">
          <el-option v-for="(item, key) in UserRoleMap" :key="key" :label="item.label" :value="Number(key)" />
        </el-select>
        <el-input v-model="searchKeyword" placeholder="搜索姓名/工号" clearable style="width:220px" :prefix-icon="Search" />
        <el-button type="primary" :icon="Plus" @click="openAddDialog">新增人员</el-button>
      </div>
    </div>

    <el-card shadow="hover">
      <el-table :data="filteredPersons" stripe style="width:100%" v-loading="loading">
        <el-table-column label="编号" prop="id" width="70" align="center" />
        <el-table-column label="姓名" prop="name" width="110">
          <template #default="{ row }">
            <div style="display:flex;align-items:center;gap:8px">
              <el-avatar :size="32" :style="{ backgroundColor: avatarColor(row.name) }">
                {{ row.name.charAt(0) }}
              </el-avatar>
              <b>{{ row.name }}</b>
            </div>
          </template>
        </el-table-column>
        <el-table-column label="工号" prop="employeeNo" width="120" align="center" />
        <el-table-column label="角色" width="100" align="center">
          <template #default="{ row }">
            <el-tag :type="UserRoleMap[row.role].type">{{ UserRoleMap[row.role].label }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="司机状态" width="100" align="center">
          <template #default="{ row }">
            <template v-if="row.role === 2">
              <el-tag :type="DriverStatusMap[row.driverStatus ?? 1].type" size="small">
                {{ DriverStatusMap[row.driverStatus ?? 1].label }}
              </el-tag>
            </template>
            <span style="color:#C0C4CC">—</span>
          </template>
        </el-table-column>
        <el-table-column label="资质状态" width="140" align="center">
          <template #default="{ row }">
            <el-tag
              v-if="row.role === 2"
              :type="getCertStatus(row).type"
              effect="dark"
              size="small"
              @click="checkQualification(row)"
              style="cursor:pointer"
            >
              {{ getCertStatus(row).text }}
            </el-tag>
            <span v-else style="color:#C0C4CC">—</span>
          </template>
        </el-table-column>
        <el-table-column label="联系电话" prop="phone" width="130" align="center" />
        <el-table-column label="入职日期" width="120" align="center">
          <template #default="{ row }">{{ row.hireDate ? formatDate(row.hireDate) : '—' }}</template>
        </el-table-column>
        <el-table-column label="状态" width="80" align="center">
          <template #default="{ row }">
            <el-switch :model-value="row.isActive" disabled size="small" />
          </template>
        </el-table-column>
        <el-table-column label="操作" width="200" fixed="right" align="center">
          <template #default="{ row }">
            <el-button type="primary" link size="small" @click="viewCertificates(row)">查看证书</el-button>
            <el-button type="success" link size="small" @click="openEditDialog(row)">编辑</el-button>
            <el-button type="danger" link size="small" @click="deletePerson(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <el-dialog v-model="personDialogVisible" :title="isEdit ? '编辑人员' : '新增人员'" width="600px">
      <el-form :model="personForm" :rules="personRules" ref="personFormRef" label-width="100px">
        <el-form-item label="姓名" prop="name">
          <el-input v-model="personForm.name" placeholder="请输入姓名" />
        </el-form-item>
        <el-form-item label="工号" prop="employeeNo">
          <el-input v-model="personForm.employeeNo" placeholder="请输入工号" />
        </el-form-item>
        <el-form-item label="身份证号" prop="idCard">
          <el-input v-model="personForm.idCard" placeholder="请输入身份证号" maxlength="18" />
        </el-form-item>
        <el-form-item label="手机号" prop="phone">
          <el-input v-model="personForm.phone" placeholder="请输入手机号" maxlength="11" />
        </el-form-item>
        <el-form-item label="角色" prop="role">
          <el-select v-model="personForm.role" placeholder="请选择角色" style="width:100%">
            <el-option v-for="(item, key) in UserRoleMap" :key="key" :label="item.label" :value="Number(key)" />
          </el-select>
        </el-form-item>
        <el-form-item label="入职日期">
          <el-date-picker v-model="personForm.hireDate" type="date" placeholder="选择日期" value-format="YYYY-MM-DD" style="width:100%" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="personDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="savePerson">保存</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="certDialogVisible" title="资质证书管理" width="720px">
      <div v-if="currentPerson" style="margin-bottom:16px">
        <el-descriptions :column="2" border size="small">
          <el-descriptions-item label="姓名">{{ currentPerson.name }}</el-descriptions-item>
          <el-descriptions-item label="工号">{{ currentPerson.employeeNo }}</el-descriptions-item>
          <el-descriptions-item label="角色">
            <el-tag :type="UserRoleMap[currentPerson.role].type" size="small">{{ UserRoleMap[currentPerson.role].label }}</el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="电话">{{ currentPerson.phone }}</el-descriptions-item>
        </el-descriptions>
      </div>

      <div class="linkage-success" v-if="qualificationIssues.length === 0 && currentPerson?.role === 2">
        <div class="success-title">
          <el-icon><CircleCheck /></el-icon>
          资质状态正常
        </div>
        <div class="success-content">所有有效证件均在有效期内，司机可以正常上岗作业。</div>
      </div>
      <div class="linkage-error" v-if="qualificationIssues.length > 0">
        <div class="error-title">
          <el-icon><CircleClose /></el-icon>
          资质异常（{{ qualificationIssues.length }} 项）
        </div>
        <div class="error-content">
          <ul style="padding-left:20px;margin:0">
            <li v-for="(issue, i) in qualificationIssues" :key="i">{{ issue }}</li>
          </ul>
          <div style="margin-top:8px;padding:8px;background:#fff;border-radius:4px">
            <b>联动规则：</b>司机证件过期将无法确认上机，任务将被自动阻止启动。
          </div>
        </div>
      </div>

      <el-divider />

      <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:12px">
        <span style="font-weight:600">证书列表</span>
        <el-button type="primary" size="small" :icon="Plus" @click="addCertificate">添加证书</el-button>
      </div>

      <el-table :data="currentPerson?.certificates || []" size="small" border>
        <el-table-column label="证件类型" width="140">
          <template #default="{ row }">{{ CertificateTypeMap[row.certificateType]?.label || '—' }}</template>
        </el-table-column>
        <el-table-column label="证件编号" prop="certificateNo" />
        <el-table-column label="发证机关" prop="issuingAuthority" />
        <el-table-column label="签发日期" width="120">
          <template #default="{ row }">{{ formatDate(row.issueDate) }}</template>
        </el-table-column>
        <el-table-column label="到期日期" width="120">
          <template #default="{ row }">
            <span :style="{ color: row.isExpired ? '#F56C6C' : (row.daysUntilExpiry <= 30 ? '#E6A23C' : '') }">
              {{ formatDate(row.expiryDate) }}
              <el-tag v-if="row.isExpired" type="danger" size="small" effect="dark" style="margin-left:4px">已过期</el-tag>
              <el-tag v-else-if="row.daysUntilExpiry <= 30" type="warning" size="small" effect="dark" style="margin-left:4px">
                {{ row.daysUntilExpiry }}天后到期
              </el-tag>
            </span>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="100" align="center">
          <template #default="{ row, $index }">
            <el-button type="danger" link size="small" @click="removeCertificate($index)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-dialog>

    <el-dialog v-model="certFormVisible" title="添加证书" width="500px">
      <el-form :model="certForm" label-width="100px" label-position="right">
        <el-form-item label="证件类型" required>
          <el-select v-model="certForm.certificateType" placeholder="请选择" style="width:100%">
            <el-option v-for="(item, key) in CertificateTypeMap" :key="key" :label="item.label" :value="Number(key)" />
          </el-select>
        </el-form-item>
        <el-form-item label="证件编号" required>
          <el-input v-model="certForm.certificateNo" />
        </el-form-item>
        <el-form-item label="发证机关" required>
          <el-input v-model="certForm.issuingAuthority" />
        </el-form-item>
        <el-form-item label="签发日期" required>
          <el-date-picker v-model="certForm.issueDate" type="date" value-format="YYYY-MM-DD" style="width:100%" />
        </el-form-item>
        <el-form-item label="到期日期" required>
          <el-date-picker v-model="certForm.expiryDate" type="date" value-format="YYYY-MM-DD" style="width:100%" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="certFormVisible = false">取消</el-button>
        <el-button type="primary" @click="confirmAddCert">确定</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, reactive, onMounted } from 'vue'
import {
  ElMessage, ElMessageBox, ElNotification, type FormInstance, type FormRules
} from 'element-plus'
import { Plus, Search, CircleCheck, CircleClose } from '@element-plus/icons-vue'
import dayjs from 'dayjs'
import api from '@/api'
import { useAppStore } from '@/store/app'
import type { Person, Certificate } from '@/api'
import {
  UserRoleMap, DriverStatusMap, CertificateTypeMap
} from '@/utils/enumMaps'

const appStore = useAppStore()
const loading = ref(false)
const saving = ref(false)

const roleFilter = ref<number | null>(null)
const searchKeyword = ref('')

const personDialogVisible = ref(false)
const certDialogVisible = ref(false)
const certFormVisible = ref(false)
const isEdit = ref(false)

const currentPerson = ref<Person | null>(null)
const qualificationIssues = ref<string[]>([])

const personForm = reactive<Partial<Person>>({
  name: '', employeeNo: '', idCard: '', phone: '', role: 2, isActive: true
})

const certForm = reactive<Partial<Certificate>>({
  certificateType: 1, certificateNo: '', issuingAuthority: '', issueDate: '', expiryDate: ''
})

const personFormRef = ref<FormInstance>()

const personRules: FormRules = {
  name: [{ required: true, message: '请输入姓名', trigger: 'blur' }],
  employeeNo: [{ required: true, message: '请输入工号', trigger: 'blur' }],
  idCard: [{ required: true, message: '请输入身份证号', trigger: 'blur' }],
  role: [{ required: true, message: '请选择角色', trigger: 'change' }]
}

const filteredPersons = computed(() => {
  let list = appStore.persons
  if (roleFilter.value !== null) {
    list = list.filter(p => p.role === roleFilter.value)
  }
  if (searchKeyword.value.trim()) {
    const kw = searchKeyword.value.trim().toLowerCase()
    list = list.filter(p =>
      p.name.toLowerCase().includes(kw) || p.employeeNo.toLowerCase().includes(kw)
    )
  }
  return list
})

const avatarColor = (name: string) => {
  const colors = ['#409EFF', '#67C23A', '#E6A23C', '#F56C6C', '#909399', '#9B59B6', '#1ABC9C']
  let hash = 0
  for (let i = 0; i < name.length; i++) hash = name.charCodeAt(i) + ((hash << 5) - hash)
  return colors[Math.abs(hash) % colors.length]
}

const formatDate = (d: string) => dayjs(d).format('YYYY-MM-DD')

const getCertStatus = (person: Person) => {
  if (person.role !== 2) return { text: '—', type: 'info' as const }
  const certs = person.certificates || []
  const operatorCerts = certs.filter(c => c.certificateType === 1 && c.isActive)
  if (!operatorCerts.length) return { text: '缺少操作证', type: 'danger' as const }
  const expired = operatorCerts.some(c => c.isExpired)
  if (expired) return { text: '证件过期', type: 'danger' as const }
  const expiring = operatorCerts.some(c => (c.daysUntilExpiry || 999) <= 30)
  if (expiring) return { text: '即将过期', type: 'warning' as const }
  return { text: '资质有效', type: 'success' as const }
}

const checkQualification = async (person: Person) => {
  const { data } = await api.persons.getQualificationIssues(person.id)
  qualificationIssues.value = data
  viewCertificates(person)
}

const viewCertificates = async (person: Person) => {
  const { data } = await api.persons.getById(person.id)
  currentPerson.value = data
  const issues = await api.persons.getQualificationIssues(person.id)
  qualificationIssues.value = person.role === 2 ? issues.data : []
  certDialogVisible.value = true
}

const openAddDialog = () => {
  isEdit.value = false
  Object.assign(personForm, {
    id: undefined, name: '', employeeNo: '', idCard: '', phone: '',
    role: 2, hireDate: '', isActive: true
  })
  personDialogVisible.value = true
}

const openEditDialog = (person: Person) => {
  isEdit.value = true
  Object.assign(personForm, { ...person })
  personDialogVisible.value = true
}

const savePerson = async () => {
  await personFormRef.value?.validate()
  saving.value = true
  try {
    if (isEdit.value && personForm.id) {
      await api.persons.update(personForm.id, personForm)
      ElMessage.success('编辑成功')
    } else {
      await api.persons.create(personForm)
      ElMessage.success('新增成功')
    }
    personDialogVisible.value = false
    await appStore.fetchPersons()
  } catch (e) {
    // 已在拦截器处理
  } finally {
    saving.value = false
  }
}

const deletePerson = async (person: Person) => {
  try {
    await ElMessageBox.confirm(
      `确定要删除人员【${person.name}】吗？此操作将停用该人员账号。`,
      '删除确认',
      { type: 'warning', confirmButtonText: '确定删除', cancelButtonText: '取消' }
    )
    await api.persons.delete(person.id)
    ElMessage.success('删除成功')
    await appStore.fetchPersons()
  } catch { /* cancel */ }
}

const addCertificate = () => {
  Object.assign(certForm, {
    certificateType: currentPerson.value?.role === 1 ? 2 : currentPerson.value?.role === 3 ? 3 : 1,
    certificateNo: '', issuingAuthority: '',
    issueDate: dayjs().format('YYYY-MM-DD'),
    expiryDate: dayjs().add(1, 'year').format('YYYY-MM-DD')
  })
  certFormVisible.value = true
}

const confirmAddCert = () => {
  if (!currentPerson.value) return
  if (!certForm.certificateNo || !certForm.issuingAuthority) {
    ElMessage.warning('请填写完整信息')
    return
  }
  currentPerson.value.certificates?.push({
    id: Date.now(),
    personId: currentPerson.value.id,
    certificateType: certForm.certificateType!,
    certificateNo: certForm.certificateNo!,
    issuingAuthority: certForm.issuingAuthority!,
    issueDate: certForm.issueDate!,
    expiryDate: certForm.expiryDate!,
    isActive: true,
    createdAt: new Date().toISOString()
  })
  ElNotification.success({ title: '添加成功', message: '请联系系统管理员同步至数据库以持久化保存' })
  certFormVisible.value = false
}

const removeCertificate = (index: number) => {
  ElMessageBox.confirm('确定删除该证书？', '确认').then(() => {
    currentPerson.value?.certificates?.splice(index, 1)
  }).catch(() => {})
}

onMounted(async () => {
  loading.value = true
  try {
    await appStore.fetchPersons()
  } finally {
    loading.value = false
  }
})
</script>
